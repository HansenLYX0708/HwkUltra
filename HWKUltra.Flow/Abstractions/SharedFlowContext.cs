using System.Collections.Concurrent;

namespace HWKUltra.Flow.Abstractions
{
    /// <summary>
    /// Shared flow context - enables cross-flow communication for parallel execution.
    /// Thread-safe: all operations use ConcurrentDictionary or SemaphoreSlim.
    /// </summary>
    public class SharedFlowContext
    {
        private readonly ConcurrentDictionary<string, object> _variables = new();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
        private readonly ConcurrentDictionary<string, SignalState> _signals = new();
        private readonly ManualResetEventSlim _pauseGate = new(true); // initially unpaused

        #region Shared Variables

        /// <summary>
        /// Set a shared variable (visible to all parallel flows)
        /// </summary>
        public void SetVariable<T>(string key, T value) where T : notnull
        {
            _variables[key] = value;
        }

        /// <summary>
        /// Get a shared variable
        /// </summary>
        public T? GetVariable<T>(string key)
        {
            if (_variables.TryGetValue(key, out var value))
            {
                if (value is T t) return t;
                if (value is string s && typeof(T) != typeof(string))
                {
                    try
                    {
                        var converted = Convert.ChangeType(s, typeof(T));
                        if (converted is T ct) return ct;
                    }
                    catch { }
                }
            }
            return default;
        }

        /// <summary>
        /// Try get a shared variable
        /// </summary>
        public bool TryGetVariable<T>(string key, out T? value)
        {
            if (_variables.TryGetValue(key, out var obj) && obj is T t)
            {
                value = t;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// Remove a shared variable
        /// </summary>
        public bool RemoveVariable(string key)
        {
            return _variables.TryRemove(key, out _);
        }

        #endregion

        #region Named Locks (Mutex)

        /// <summary>
        /// Acquire a named lock. Only one flow can hold a given lock at a time.
        /// </summary>
        public async Task<bool> AcquireLockAsync(string name, int timeoutMs = -1, CancellationToken cancellationToken = default)
        {
            var semaphore = _locks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1));
            return await semaphore.WaitAsync(timeoutMs, cancellationToken);
        }

        /// <summary>
        /// Release a named lock
        /// </summary>
        public void ReleaseLock(string name)
        {
            if (_locks.TryGetValue(name, out var semaphore))
            {
                try { semaphore.Release(); }
                catch (SemaphoreFullException) { }
            }
        }

        #endregion

        #region Signals (Event-based cross-flow notification)

        /// <summary>
        /// Set a signal, waking up all waiters
        /// </summary>
        public void SetSignal(string name, object? value = null)
        {
            var state = _signals.GetOrAdd(name, _ => new SignalState());
            lock (state)
            {
                state.Value = value;
                state.IsSet = true;
                // Complete the current TCS and create a new one for future waiters
                state.Tcs.TrySetResult(value);
            }
        }

        /// <summary>
        /// Reset a signal (clear it so future waiters will block)
        /// </summary>
        public void ResetSignal(string name)
        {
            if (_signals.TryGetValue(name, out var state))
            {
                lock (state)
                {
                    state.IsSet = false;
                    // Replace TCS only if completed
                    if (state.Tcs.Task.IsCompleted)
                    {
                        state.Tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                    }
                }
            }
        }

        /// <summary>
        /// Wait for a signal to be set. Returns the signal value.
        /// </summary>
        public async Task<object?> WaitForSignalAsync(string name, int timeoutMs = -1, CancellationToken cancellationToken = default)
        {
            var state = _signals.GetOrAdd(name, _ => new SignalState());
            Task<object?> waitTask;

            lock (state)
            {
                if (state.IsSet)
                    return state.Value;

                waitTask = state.Tcs.Task;
            }

            if (timeoutMs < 0)
            {
                // Infinite wait with cancellation
                var cancelTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                using var reg = cancellationToken.Register(() => cancelTcs.TrySetCanceled());
                var completed = await Task.WhenAny(waitTask, cancelTcs.Task);
                return await completed;
            }
            else
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutMs);
                var cancelTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                using var reg = cts.Token.Register(() => cancelTcs.TrySetCanceled());
                // Let OperationCanceledException propagate on timeout
                // so callers (e.g. WaitForSignalNode) can distinguish timeout from signal received
                var completed = await Task.WhenAny(waitTask, cancelTcs.Task);
                return await completed;
            }
        }

        /// <summary>
        /// Check if a signal is currently set (non-blocking)
        /// </summary>
        public bool IsSignalSet(string name)
        {
            return _signals.TryGetValue(name, out var state) && state.IsSet;
        }

        #endregion

        #region Shared Pause/Resume

        /// <summary>
        /// Whether the shared context is currently paused.
        /// </summary>
        public bool IsPaused => !_pauseGate.IsSet;

        /// <summary>
        /// Pause all flows sharing this context.
        /// </summary>
        public void Pause()
        {
            _pauseGate.Reset();
        }

        /// <summary>
        /// Resume all flows sharing this context.
        /// </summary>
        public void Resume()
        {
            _pauseGate.Set();
        }

        /// <summary>
        /// Wait if paused (should be called by FlowEngine before executing each node).
        /// </summary>
        public void WaitIfPaused(CancellationToken cancellationToken = default)
        {
            _pauseGate.Wait(cancellationToken);
        }

        #endregion

        /// <summary>
        /// Internal signal state
        /// </summary>
        private class SignalState
        {
            public bool IsSet;
            public object? Value;
            public TaskCompletionSource<object?> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
