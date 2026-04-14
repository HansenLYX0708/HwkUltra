using System.Text.Json;
using HWKUltra.Camera;
using HWKUltra.Camera.Abstractions;
using HWKUltra.Camera.Core;
using HWKUltra.Camera.Implementations;
using HWKUltra.Camera.Implementations.basler;

namespace HWKUltra.Builder
{
    /// <summary>
    /// Generic camera builder, supports any vendor's camera controller config
    /// (corresponds to MotionBuilder&lt;TConfig&gt; / LightSourceBuilder&lt;TConfig&gt;).
    /// </summary>
    public class CameraBuilder<TConfig> where TConfig : class
    {
        private TConfig? _config;
        private readonly Func<TConfig, ICameraController> _controllerFactory;
        private readonly Func<TConfig, Dictionary<string, CameraConfig>>? _cameraMapExtractor;
        private Func<string, TConfig>? _jsonDeserializer;

        public CameraBuilder(
            Func<TConfig, ICameraController> controllerFactory,
            Func<TConfig, Dictionary<string, CameraConfig>>? cameraMapExtractor = null)
        {
            _controllerFactory = controllerFactory;
            _cameraMapExtractor = cameraMapExtractor;
        }

        public CameraBuilder<TConfig> WithJsonDeserializer(Func<string, TConfig> deserializer)
        {
            _jsonDeserializer = deserializer;
            return this;
        }

        public CameraBuilder<TConfig> FromJson(string json)
        {
            if (_jsonDeserializer != null)
            {
                _config = _jsonDeserializer(json);
            }
            else
            {
                throw new InvalidOperationException(
                    "Json deserializer not configured. " +
                    "Call WithJsonDeserializer() or use dedicated builder (BaslerCameraBuilder).");
            }
            return this;
        }

        public CameraBuilder<TConfig> FromJsonFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        public ICameraController BuildController()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson or FromJsonFile first.");

            return _controllerFactory(_config);
        }

        public CameraRouter BuildRouter()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson or FromJsonFile first.");

            var controller = _controllerFactory(_config);
            var cameraMap = _cameraMapExtractor?.Invoke(_config)
                ?? new Dictionary<string, CameraConfig>();

            return new CameraRouter(controller, cameraMap);
        }
    }

    /// <summary>
    /// Basler camera dedicated builder (corresponds to MotionBuilder / CcsLightSourceBuilder).
    /// </summary>
    public class BaslerCameraBuilder
    {
        private readonly CameraBuilder<BaslerCameraControllerConfig> _inner;

        public BaslerCameraBuilder()
        {
            _inner = new CameraBuilder<BaslerCameraControllerConfig>(
                cfg => new BaslerCameraController(cfg),
                cfg => cfg.Cameras.ToDictionary(c => c.Name, c => c));

            // Use source generator for JSON deserialization
            _inner.WithJsonDeserializer(json =>
                JsonSerializer.Deserialize(json, CameraJsonContext.Default.BaslerCameraControllerConfig)!);
        }

        public BaslerCameraBuilder FromJson(string json)
        {
            _inner.FromJson(json);
            return this;
        }

        public BaslerCameraBuilder FromJsonFile(string filePath)
        {
            _inner.FromJsonFile(filePath);
            return this;
        }

        public BaslerCameraController BuildController() =>
            (BaslerCameraController)_inner.BuildController();

        public CameraRouter BuildRouter() => _inner.BuildRouter();
    }
}
