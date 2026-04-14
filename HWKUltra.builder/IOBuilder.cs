using System.Text.Json;
using HWKUltra.DeviceIO;
using HWKUltra.DeviceIO.Abstractions;
using HWKUltra.DeviceIO.Core;
using HWKUltra.DeviceIO.Implementations;
using HWKUltra.DeviceIO.Implementations.galil;

namespace HWKUltra.Builder
{
    /// <summary>
    /// Generic IO builder, supports any vendor's IO controller config (corresponds to MotionBuilder&lt;TConfig&gt;).
    /// </summary>
    public class IOBuilder<TConfig> where TConfig : class
    {
        private TConfig? _config;
        private readonly Func<TConfig, IIOController> _controllerFactory;
        private readonly Func<TConfig, List<IOPointConfig>>? _inputsExtractor;
        private readonly Func<TConfig, List<IOPointConfig>>? _outputsExtractor;
        private readonly Func<TConfig, int>? _monitorIntervalExtractor;

        private Func<string, TConfig>? _jsonDeserializer;

        public IOBuilder(
            Func<TConfig, IIOController> controllerFactory,
            Func<TConfig, List<IOPointConfig>>? inputsExtractor = null,
            Func<TConfig, List<IOPointConfig>>? outputsExtractor = null,
            Func<TConfig, int>? monitorIntervalExtractor = null)
        {
            _controllerFactory = controllerFactory;
            _inputsExtractor = inputsExtractor;
            _outputsExtractor = outputsExtractor;
            _monitorIntervalExtractor = monitorIntervalExtractor;
        }

        public IOBuilder<TConfig> WithJsonDeserializer(Func<string, TConfig> deserializer)
        {
            _jsonDeserializer = deserializer;
            return this;
        }

        public IOBuilder<TConfig> FromJson(string json)
        {
            if (_jsonDeserializer != null)
            {
                _config = _jsonDeserializer(json);
            }
            else
            {
                throw new InvalidOperationException(
                    "Json deserializer not configured. " +
                    "Call WithJsonDeserializer() or use dedicated builder (GalilIOBuilder).");
            }
            return this;
        }

        public IOBuilder<TConfig> FromJsonFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        public IIOController BuildController()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson or FromJsonFile first.");

            return _controllerFactory(_config);
        }

        public IORouter BuildRouter()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson or FromJsonFile first.");

            var controller = _controllerFactory(_config);
            var inputs = _inputsExtractor?.Invoke(_config) ?? new List<IOPointConfig>();
            var outputs = _outputsExtractor?.Invoke(_config) ?? new List<IOPointConfig>();
            var interval = _monitorIntervalExtractor?.Invoke(_config) ?? 100;

            return new IORouter(controller, inputs, outputs, interval);
        }
    }

    /// <summary>
    /// Galil IO dedicated builder (corresponds to MotionBuilder / GtsMotionBuilder).
    /// </summary>
    public class GalilIOBuilder
    {
        private readonly IOBuilder<GalilIOConfig> _inner;
        private GalilIOConfig? _config;

        public GalilIOBuilder()
        {
            _inner = new IOBuilder<GalilIOConfig>(
                cfg => new GalilIOController(cfg),
                cfg => cfg.Inputs,
                cfg => cfg.Outputs,
                cfg => cfg.MonitorIntervalMs);

            _inner.WithJsonDeserializer(json =>
            {
                var config = JsonSerializer.Deserialize(json, IOJsonContext.Default.GalilIOConfig)!;
                _config = config;
                return config;
            });
        }

        public GalilIOBuilder FromJson(string json)
        {
            _inner.FromJson(json);
            return this;
        }

        public GalilIOBuilder FromJsonFile(string filePath)
        {
            _inner.FromJsonFile(filePath);
            return this;
        }

        public GalilIOController BuildController() =>
            (GalilIOController)_inner.BuildController();

        public IORouter BuildRouter() => _inner.BuildRouter();

        /// <summary>
        /// Build IORouter, auto-connect, and set default outputs.
        /// </summary>
        public IORouter BuildAndConnect()
        {
            var router = _inner.BuildRouter();
            // Open controller (already encapsulated in router's controller)
            var controller = (GalilIOController)_inner.BuildController();
            controller.Open();

            // Set default-on outputs
            if (_config?.DefaultOnOutputs != null)
            {
                foreach (var outputName in _config.DefaultOnOutputs)
                {
                    if (router.HasOutput(outputName))
                    {
                        router.SetOutput(outputName, true);
                    }
                }
            }

            return router;
        }
    }
}
