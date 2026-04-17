using System.Text.Json;
using HWKUltra.Tray;
using HWKUltra.Tray.Abstractions;
using HWKUltra.Tray.Core;
using HWKUltra.Tray.Implementations.WDTray;

namespace HWKUltra.Builder
{
    /// <summary>
    /// Generic tray builder for constructing controller and router from configuration.
    /// </summary>
    public class TrayBuilder<TConfig> where TConfig : class
    {
        private TConfig? _config;
        private readonly Func<TConfig, ITrayController> _controllerFactory;
        private readonly Func<TConfig, Dictionary<string, object>> _instanceMapFactory;
        private Func<string, TConfig>? _jsonDeserializer;

        public TrayBuilder(
            Func<TConfig, ITrayController> controllerFactory,
            Func<TConfig, Dictionary<string, object>> instanceMapFactory)
        {
            _controllerFactory = controllerFactory;
            _instanceMapFactory = instanceMapFactory;
        }

        public TrayBuilder<TConfig> WithJsonDeserializer(Func<string, TConfig> deserializer)
        {
            _jsonDeserializer = deserializer;
            return this;
        }

        public TrayBuilder<TConfig> FromJson(string json)
        {
            if (_jsonDeserializer == null)
                throw new InvalidOperationException("JSON deserializer not configured. Call WithJsonDeserializer first.");
            _config = _jsonDeserializer(json);
            return this;
        }

        public ITrayController BuildController()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson first.");
            return _controllerFactory(_config);
        }

        public TrayRouter BuildRouter()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson first.");
            var controller = _controllerFactory(_config);
            var instanceMap = _instanceMapFactory(_config);
            return new TrayRouter(controller, instanceMap);
        }
    }

    /// <summary>
    /// Dedicated tray builder for the default TrayController implementation.
    /// </summary>
    public class TrayBuilder
    {
        private TrayControllerConfig? _config;

        public TrayBuilder FromJson(string json)
        {
            _config = JsonSerializer.Deserialize(json, TrayJsonContext.Default.TrayControllerConfig);
            if (_config == null)
                throw new InvalidOperationException("Failed to deserialize TrayControllerConfig");
            return this;
        }

        public TrayController BuildController()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson first.");
            return new TrayController(_config);
        }

        public TrayRouter BuildRouter()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson first.");
            var controller = new TrayController(_config);
            var instanceMap = _config.Instances.ToDictionary(i => i.Name, i => (object)i);
            return new TrayRouter(controller, instanceMap);
        }
    }
}
