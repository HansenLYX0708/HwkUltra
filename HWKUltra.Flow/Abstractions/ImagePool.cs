using System.Collections.Concurrent;
using System.Drawing;

namespace HWKUltra.Flow.Abstractions
{
    /// <summary>
    /// A single frame handed out by an <see cref="ImagePool"/>. The consumer owns the
    /// <see cref="Bitmap"/> and is responsible for disposing it (use the DisposeImage node
    /// at the end of a worker flow, or call <see cref="Dispose"/>).
    /// </summary>
    public sealed class PoolItem : IDisposable
    {
        public Bitmap Bitmap { get; }
        public int Index { get; }
        public DateTime CapturedAt { get; }
        public string Source { get; }

        public PoolItem(Bitmap bitmap, int index, DateTime capturedAt, string source)
        {
            Bitmap = bitmap;
            Index = index;
            CapturedAt = capturedAt;
            Source = source;
        }

        public void Dispose() => Bitmap.Dispose();
    }

    /// <summary>
    /// Named, bounded, thread-safe image queue with a completion signal. Producers (e.g.
    /// a camera capture node) push frames via <see cref="TryAdd"/>; consumers (e.g. a
    /// streaming <c>Parallel</c> node) pull via <see cref="GetConsumingEnumerable"/>.
    /// When the producer is done it calls <see cref="CompleteAdding"/> and the consumer
    /// loop exits naturally once the queue drains.
    /// </summary>
    public sealed class ImagePool : IDisposable
    {
        private BlockingCollection<PoolItem> _queue;
        private readonly object _resetLock = new();
        private long _totalEnqueued;
        private long _totalDequeued;
        private long _totalDropped;
        private int _nextIndex;

        public string Name { get; }
        public int Capacity { get; }

        public long TotalEnqueued => Interlocked.Read(ref _totalEnqueued);
        public long TotalDequeued => Interlocked.Read(ref _totalDequeued);
        public long TotalDropped  => Interlocked.Read(ref _totalDropped);
        public int CurrentCount   => _queue.Count;
        public bool IsAddingCompleted => _queue.IsAddingCompleted;
        public bool IsCompleted => _queue.IsCompleted;

        public ImagePool(string name, int capacity)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Pool name required", nameof(name));
            if (capacity <= 0) capacity = 50;
            Name = name;
            Capacity = capacity;
            _queue = new BlockingCollection<PoolItem>(capacity);
        }

        /// <summary>
        /// Producer API: try to add one frame. When the queue is full, blocks up to
        /// <paramref name="timeoutMs"/> ms (or forever if -1). Returns false on timeout
        /// or if CompleteAdding has already been called.
        /// </summary>
        public bool TryAdd(Bitmap bitmap, int timeoutMs = 100, string source = "", CancellationToken ct = default)
        {
            if (_queue.IsAddingCompleted)
            {
                bitmap.Dispose();
                Interlocked.Increment(ref _totalDropped);
                return false;
            }

            var idx = Interlocked.Increment(ref _nextIndex) - 1;
            var item = new PoolItem(bitmap, idx, DateTime.Now, source);
            try
            {
                if (_queue.TryAdd(item, timeoutMs, ct))
                {
                    Interlocked.Increment(ref _totalEnqueued);
                    return true;
                }
                // Timed out: dispose and drop.
                item.Dispose();
                Interlocked.Increment(ref _totalDropped);
                return false;
            }
            catch (InvalidOperationException)
            {
                // CompleteAdding was called concurrently.
                item.Dispose();
                Interlocked.Increment(ref _totalDropped);
                return false;
            }
        }

        /// <summary>
        /// Producer API: mark the pool as having no more frames. Consumers will drain
        /// remaining items then exit their enumeration loops.
        /// </summary>
        public void CompleteAdding()
        {
            if (!_queue.IsAddingCompleted)
                _queue.CompleteAdding();
        }

        /// <summary>
        /// Reset the pool so it can accept new frames after <see cref="CompleteAdding"/>
        /// was called. Drains and disposes any remaining items, then replaces the internal
        /// queue. Statistics (<see cref="TotalEnqueued"/> etc.) are preserved across resets.
        /// Safe for the sequential capture-then-process pattern (e.g. Calibrate flows that
        /// capture + FindDatum multiple times into the same named pool).
        /// </summary>
        public void Reset()
        {
            lock (_resetLock)
            {
                // Drain remaining items
                while (_queue.TryTake(out var leftover))
                    leftover.Dispose();
                if (_queue.IsAddingCompleted)
                {
                    _queue.Dispose();
                    _queue = new BlockingCollection<PoolItem>(Capacity);
                }
            }
        }

        /// <summary>
        /// Consumer API: yields items as they become available. Blocks on an empty
        /// queue and exits when the pool is completed + drained, or when cancelled.
        /// Each returned item counts as one dequeue (for stats).
        /// </summary>
        public IEnumerable<PoolItem> GetConsumingEnumerable(CancellationToken ct)
        {
            foreach (var item in _queue.GetConsumingEnumerable(ct))
            {
                Interlocked.Increment(ref _totalDequeued);
                yield return item;
            }
        }

        public void Dispose()
        {
            CompleteAdding();
            // Drain and dispose any remaining bitmaps so they don't leak.
            while (_queue.TryTake(out var item))
                item.Dispose();
            _queue.Dispose();
        }

        public override string ToString()
            => $"ImagePool[{Name}] enq={TotalEnqueued} deq={TotalDequeued} drop={TotalDropped} inQueue={CurrentCount} done={IsAddingCompleted}";
    }

    /// <summary>
    /// Convenience extensions for registering/locating <see cref="ImagePool"/> instances
    /// in a <see cref="SharedFlowContext"/>. The pool is stored under the same key as its
    /// Name so flows can reference it by name (e.g. <c>ItemsSource=ImagePool</c>).
    /// </summary>
    public static class ImagePoolExtensions
    {
        public static ImagePool CreatePool(this SharedFlowContext shared, string name, int capacity)
        {
            if (shared == null) throw new ArgumentNullException(nameof(shared));
            var pool = new ImagePool(name, capacity);
            shared.SetVariable(name, pool);
            return pool;
        }

        public static ImagePool? GetPool(this SharedFlowContext shared, string name)
            => shared.GetVariable<ImagePool>(name);

        public static bool RemovePool(this SharedFlowContext shared, string name)
        {
            var pool = shared.GetPool(name);
            if (pool == null) return false;
            pool.Dispose();
            shared.RemoveVariable(name);
            return true;
        }
    }
}
