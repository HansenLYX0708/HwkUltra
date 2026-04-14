using System.Text.Json;
using HWKUltra.AutoFocus;
using HWKUltra.AutoFocus.Abstractions;
using HWKUltra.AutoFocus.Core;
using HWKUltra.AutoFocus.Implementations;
using HWKUltra.AutoFocus.Implementations.laf;

namespace HWKUltra.Builder
{
    /// <summary>
    /// Generic auto focus builder, supports any vendor's AF controller config
    /// (corresponds to MotionBuilder / LightSourceBuilder / CameraBuilder).
    /// </summary>
    public class AutoFocusBuilder<TConfig> where TConfig : class
    {
        private TConfig? _config;
        private readonly Func<TConfig, IAutoFocusController> _controllerFactory;
        private readonly Func<TConfig, Dictionary<string, AutoFocusConfig>>? _instanceMapExtractor;
        private Func<string, TConfig>? _jsonDeserializer;

        public AutoFocusBuilder(
            Func<TConfig, IAutoFocusController> controllerFactory,
            Func<TConfig, Dictionary<string, AutoFocusConfig>>? instanceMapExtractor = null)
        {
            _controllerFactory = controllerFactory;
            _instanceMapExtractor = instanceMapExtractor;
        }

        public AutoFocusBuilder<TConfig> WithJsonDeserializer(Func<string, TConfig> deserializer)
        {
            _jsonDeserializer = deserializer;
            return this;
        }

        public AutoFocusBuilder<TConfig> FromJson(string json)
        {
            if (_jsonDeserializer != null)
            {
                _config = _jsonDeserializer(json);
            }
            else
            {
                throw new InvalidOperationException(
                    "Json deserializer not configured. " +
                    "Call WithJsonDeserializer() or use dedicated builder (LafAutoFocusBuilder).");
            }
            return this;
        }

        public AutoFocusBuilder<TConfig> FromJsonFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        public IAutoFocusController BuildController()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson or FromJsonFile first.");

            return _controllerFactory(_config);
        }

        public AutoFocusRouter BuildRouter()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson or FromJsonFile first.");

            var controller = _controllerFactory(_config);
            var instanceMap = _instanceMapExtractor?.Invoke(_config)
                ?? new Dictionary<string, AutoFocusConfig>();

            return new AutoFocusRouter(controller, instanceMap);
        }
    }

    /// <summary>
    /// LAF auto focus dedicated builder (corresponds to BaslerCameraBuilder / CcsLightSourceBuilder).
    /// </summary>
    public class LafAutoFocusBuilder
    {
        private readonly AutoFocusBuilder<LafAutoFocusControllerConfig> _inner;

        public LafAutoFocusBuilder()
        {
            _inner = new AutoFocusBuilder<LafAutoFocusControllerConfig>(
                cfg => new LafAutoFocusController(cfg),
                cfg => cfg.Instances.ToDictionary(i => i.Name, i => i));

            _inner.WithJsonDeserializer(json =>
                JsonSerializer.Deserialize(json, AutoFocusJsonContext.Default.LafAutoFocusControllerConfig)!);
        }

        public LafAutoFocusBuilder FromJson(string json)
        {
            _inner.FromJson(json);
            return this;
        }

        public LafAutoFocusBuilder FromJsonFile(string filePath)
        {
            _inner.FromJsonFile(filePath);
            return this;
        }

        public LafAutoFocusController BuildController() =>
            (LafAutoFocusController)_inner.BuildController();

        public AutoFocusRouter BuildRouter() => _inner.BuildRouter();
    }
}
