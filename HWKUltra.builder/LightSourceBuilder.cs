using System.Text.Json;
using HWKUltra.LightSource;
using HWKUltra.LightSource.Abstractions;
using HWKUltra.LightSource.Core;
using HWKUltra.LightSource.Implementations;
using HWKUltra.LightSource.Implementations.ccs;

namespace HWKUltra.Builder
{
    /// <summary>
    /// Generic light source builder, supports any vendor's light controller config
    /// (corresponds to MotionBuilder&lt;TConfig&gt;).
    /// </summary>
    public class LightSourceBuilder<TConfig> where TConfig : class
    {
        private TConfig? _config;
        private readonly Func<TConfig, ILightSourceController> _controllerFactory;
        private readonly Func<TConfig, Dictionary<string, LightChannelConfig>>? _channelMapExtractor;
        private Func<string, TConfig>? _jsonDeserializer;

        public LightSourceBuilder(
            Func<TConfig, ILightSourceController> controllerFactory,
            Func<TConfig, Dictionary<string, LightChannelConfig>>? channelMapExtractor = null)
        {
            _controllerFactory = controllerFactory;
            _channelMapExtractor = channelMapExtractor;
        }

        public LightSourceBuilder<TConfig> WithJsonDeserializer(Func<string, TConfig> deserializer)
        {
            _jsonDeserializer = deserializer;
            return this;
        }

        public LightSourceBuilder<TConfig> FromJson(string json)
        {
            if (_jsonDeserializer != null)
            {
                _config = _jsonDeserializer(json);
            }
            else
            {
                throw new InvalidOperationException(
                    "Json deserializer not configured. " +
                    "Call WithJsonDeserializer() or use dedicated builder (CcsLightSourceBuilder).");
            }
            return this;
        }

        public LightSourceBuilder<TConfig> FromJsonFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        public ILightSourceController BuildController()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson or FromJsonFile first.");

            return _controllerFactory(_config);
        }

        public LightSourceRouter BuildRouter()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson or FromJsonFile first.");

            var controller = _controllerFactory(_config);
            var channelMap = _channelMapExtractor?.Invoke(_config)
                ?? new Dictionary<string, LightChannelConfig>();

            return new LightSourceRouter(controller, channelMap);
        }
    }

    /// <summary>
    /// CCS light source dedicated builder (corresponds to MotionBuilder / GtsMotionBuilder).
    /// </summary>
    public class CcsLightSourceBuilder
    {
        private readonly LightSourceBuilder<CcsLightSourceControllerConfig> _inner;

        public CcsLightSourceBuilder()
        {
            _inner = new LightSourceBuilder<CcsLightSourceControllerConfig>(
                cfg => new CcsLightSourceController(cfg),
                cfg => cfg.Channels.ToDictionary(c => c.Name, c => c));

            // Use source generator for JSON deserialization
            _inner.WithJsonDeserializer(json =>
                JsonSerializer.Deserialize(json, LightSourceJsonContext.Default.CcsLightSourceControllerConfig)!);
        }

        public CcsLightSourceBuilder FromJson(string json)
        {
            _inner.FromJson(json);
            return this;
        }

        public CcsLightSourceBuilder FromJsonFile(string filePath)
        {
            _inner.FromJsonFile(filePath);
            return this;
        }

        public CcsLightSourceController BuildController() =>
            (CcsLightSourceController)_inner.BuildController();

        public LightSourceRouter BuildRouter() => _inner.BuildRouter();
    }
}
