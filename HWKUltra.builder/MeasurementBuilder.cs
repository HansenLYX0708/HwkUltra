using System.Text.Json;
using HWKUltra.Measurement;
using HWKUltra.Measurement.Abstractions;
using HWKUltra.Measurement.Core;
using HWKUltra.Measurement.Implementations;
using HWKUltra.Measurement.Implementations.Keyence;

namespace HWKUltra.Builder
{
    /// <summary>
    /// Generic builder for creating measurement controllers and routers from configuration.
    /// </summary>
    public class MeasurementBuilder<TConfig> where TConfig : class
    {
        private readonly Func<TConfig, IMeasurementController> _controllerFactory;
        private readonly Func<TConfig, Dictionary<string, MeasurementConfig>> _instanceMapFactory;
        private Func<string, TConfig>? _jsonDeserializer;
        private TConfig? _config;

        public MeasurementBuilder(
            Func<TConfig, IMeasurementController> controllerFactory,
            Func<TConfig, Dictionary<string, MeasurementConfig>> instanceMapFactory)
        {
            _controllerFactory = controllerFactory;
            _instanceMapFactory = instanceMapFactory;
        }

        public MeasurementBuilder<TConfig> WithJsonDeserializer(Func<string, TConfig> deserializer)
        {
            _jsonDeserializer = deserializer;
            return this;
        }

        public MeasurementBuilder<TConfig> FromJson(string json)
        {
            if (_jsonDeserializer == null)
                throw new InvalidOperationException("JSON deserializer not configured. Call WithJsonDeserializer first.");
            _config = _jsonDeserializer(json);
            return this;
        }

        public IMeasurementController BuildController()
        {
            if (_config == null) throw new InvalidOperationException("Configuration not set. Call FromJson first.");
            return _controllerFactory(_config);
        }

        public MeasurementRouter BuildRouter()
        {
            if (_config == null) throw new InvalidOperationException("Configuration not set. Call FromJson first.");
            var controller = _controllerFactory(_config);
            var instanceMap = _instanceMapFactory(_config);
            return new MeasurementRouter(controller, instanceMap);
        }
    }

    /// <summary>
    /// Dedicated builder for Keyence CL3-IF measurement controllers.
    /// </summary>
    public class KeyenceMeasurementBuilder
    {
        private KeyenceMeasurementControllerConfig? _config;

        public KeyenceMeasurementBuilder FromJson(string json)
        {
            _config = JsonSerializer.Deserialize(json, MeasurementJsonContext.Default.KeyenceMeasurementControllerConfig)
                ?? throw new InvalidOperationException("Failed to deserialize KeyenceMeasurementControllerConfig");
            return this;
        }

        public KeyenceMeasurementController BuildController()
        {
            if (_config == null) throw new InvalidOperationException("Configuration not set. Call FromJson first.");
            return new KeyenceMeasurementController(_config);
        }

        public MeasurementRouter BuildRouter()
        {
            if (_config == null) throw new InvalidOperationException("Configuration not set. Call FromJson first.");
            var controller = new KeyenceMeasurementController(_config);
            var instanceMap = _config.Instances.ToDictionary(i => i.Name, i => i);
            return new MeasurementRouter(controller, instanceMap);
        }
    }
}
