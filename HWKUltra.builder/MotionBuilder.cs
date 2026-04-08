using System.Text.Json;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Core;
using HWKUltra.Motion.Implementations.elmo;

namespace HWKUltra.Builder
{
    /// <summary>
    /// 泛型构建器，支持任意品牌的控制器配置
    /// </summary>
    public class MotionBuilder<TConfig> where TConfig : class
    {
        private TConfig? _config;
        private readonly Dictionary<string, ISingleAxis> _singleAxes = new();
        private readonly Dictionary<string, int> _axisMap = new();
        private readonly Func<TConfig, IMotionController> _controllerFactory;
        private readonly Func<TConfig, Dictionary<string, int>>? _axisMapExtractor;

        public MotionBuilder(
            Func<TConfig, IMotionController> controllerFactory,
            Func<TConfig, Dictionary<string, int>>? axisMapExtractor = null)
        {
            _controllerFactory = controllerFactory;
            _axisMapExtractor = axisMapExtractor;
        }

        public MotionBuilder<TConfig> FromJson(string json)
        {
            _config = JsonSerializer.Deserialize<TConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return this;
        }

        public MotionBuilder<TConfig> FromJsonFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            return FromJson(json);
        }

        public MotionBuilder<TConfig> AddSingleAxis(string name, ISingleAxis axis)
        {
            _singleAxes[name] = axis;
            return this;
        }

        public MotionBuilder<TConfig> WithAxisMap(Dictionary<string, int> axisMap)
        {
            foreach (var item in axisMap)
                _axisMap[item.Key] = item.Value;
            return this;
        }

        public IMotionController BuildController()
        {
            if (_config == null)
                throw new InvalidOperationException("Configuration not loaded. Call FromJson or FromJsonFile first.");

            return _controllerFactory(_config);
        }

        public MotionRouter BuildRouter()
        {
            var controller = BuildController();
            var axisMap = _axisMapExtractor != null
                ? _axisMapExtractor(_config!)
                : _axisMap;
            return new MotionRouter(controller, axisMap, _singleAxes);
        }
    }

    /// <summary>
    /// Elmo 专用构建器（保持后向兼容）
    /// </summary>
    public class MotionBuilder
    {
        private readonly MotionBuilder<ElmoMotionControllerConfig> _inner;

        public MotionBuilder()
        {
            _inner = new MotionBuilder<ElmoMotionControllerConfig>(
                cfg => new ElmoMotionController(cfg),
                cfg => cfg.Axes.Select((axis, index) => new { axis.Name, Index = index })
                              .ToDictionary(x => x.Name, x => x.Index));
        }

        public MotionBuilder FromJson(string json)
        {
            _inner.FromJson(json);
            return this;
        }

        public MotionBuilder FromJsonFile(string filePath)
        {
            _inner.FromJsonFile(filePath);
            return this;
        }

        public MotionBuilder AddSingleAxis(string name, ISingleAxis axis)
        {
            _inner.AddSingleAxis(name, axis);
            return this;
        }

        public ElmoMotionController BuildController() =>
            (ElmoMotionController)_inner.BuildController();

        public MotionRouter BuildRouter() => _inner.BuildRouter();
    }
}
