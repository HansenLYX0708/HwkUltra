using System.Text.Json;
using HWKUltra.Communication;
using HWKUltra.Communication.Abstractions;
using HWKUltra.Communication.Core;
using HWKUltra.Communication.Implementations.WDConnect;

namespace HWKUltra.Builder
{
    /// <summary>
    /// Generic communication builder for constructing controller and router from configuration.
    /// </summary>
    public class CommunicationBuilder<TConfig> where TConfig : class
    {
        private TConfig? _config;
        private readonly Func<TConfig, ICommunicationController> _controllerFactory;
        private Func<string, TConfig>? _jsonDeserializer;

        public CommunicationBuilder(Func<TConfig, ICommunicationController> controllerFactory)
        {
            _controllerFactory = controllerFactory;
        }

        public CommunicationBuilder<TConfig> WithJsonDeserializer(Func<string, TConfig> deserializer)
        {
            _jsonDeserializer = deserializer;
            return this;
        }

        public CommunicationBuilder<TConfig> FromJson(string json)
        {
            if (_jsonDeserializer == null)
                throw new InvalidOperationException("JSON deserializer not configured. Call WithJsonDeserializer first.");
            _config = _jsonDeserializer(json);
            return this;
        }

        public ICommunicationController BuildController()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson first.");
            return _controllerFactory(_config);
        }

        public CommunicationRouter BuildRouter()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson first.");
            var controller = _controllerFactory(_config);
            return new CommunicationRouter(controller);
        }
    }

    /// <summary>
    /// Dedicated builder for WDConnect-based communication (CamStar/MES).
    /// </summary>
    public class WDConnectCommunicationBuilder
    {
        private WDConnectCommunicationControllerConfig? _config;

        public WDConnectCommunicationBuilder FromJson(string json)
        {
            _config = JsonSerializer.Deserialize(json, CommunicationJsonContext.Default.WDConnectCommunicationControllerConfig);
            if (_config == null)
                throw new InvalidOperationException("Failed to deserialize WDConnectCommunicationControllerConfig");
            return this;
        }

        public WDConnectCommunicationBuilder FromJsonFile(string path)
        {
            var json = File.ReadAllText(path);
            return FromJson(json);
        }

        public WDConnectCommunicationController BuildController()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson first.");
            return new WDConnectCommunicationController(_config);
        }

        public CommunicationRouter BuildRouter()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson first.");
            var controller = new WDConnectCommunicationController(_config);
            return new CommunicationRouter(controller);
        }
    }
}
