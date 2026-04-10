using System.Text.Json;
using HWKUltra.Motion;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Core;
using HWKUltra.Motion.Implementations.elmo;
using HWKUltra.Motion.Implementations.gts;

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

        private Func<string, TConfig>? _jsonDeserializer;

        public MotionBuilder<TConfig> WithJsonDeserializer(Func<string, TConfig> deserializer)
        {
            _jsonDeserializer = deserializer;
            return this;
        }

        public MotionBuilder<TConfig> FromJson(string json)
        {
            if (_jsonDeserializer != null)
            {
                _config = _jsonDeserializer(json);
            }
            else
            {
                throw new InvalidOperationException(
                    "Json deserializer not configured. " +
                    "Call WithJsonDeserializer() or use dedicated builder (MotionBuilder/GtsMotionBuilder).");
            }
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

        private Dictionary<string, string[]> _groupAxesMap = new();
        private Func<TConfig, Dictionary<string, string[]>>? _groupAxesMapExtractor;

        public MotionBuilder<TConfig> WithGroupAxesMap(Dictionary<string, string[]> groupAxesMap)
        {
            _groupAxesMap = groupAxesMap;
            return this;
        }

        public MotionBuilder<TConfig> WithGroupAxesExtractor(Func<TConfig, Dictionary<string, string[]>> extractor)
        {
            _groupAxesMapExtractor = extractor;
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
            var groupAxesMap = _groupAxesMapExtractor != null
                ? _groupAxesMapExtractor(_config!)
                : _groupAxesMap;
            return new MotionRouter(controller, axisMap, _singleAxes, groupAxesMap);
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

            // 使用源生成器进行JSON反序列化
            _inner.WithJsonDeserializer(json =>
                JsonSerializer.Deserialize(json, MotionJsonContext.Default.ElmoMotionControllerConfig)!);

            // 配置组轴映射提取器
            _inner.WithGroupAxesExtractor(cfg =>
                cfg.Groups.ToDictionary(g => g.Name, g => g.Axes.ToArray()));
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

    /// <summary>
    /// 固高GTS专用构建器
    /// </summary>
    public class GtsMotionBuilder
    {
        private readonly MotionBuilder<GtsMotionControllerConfig> _inner;

        public GtsMotionBuilder()
        {
            _inner = new MotionBuilder<GtsMotionControllerConfig>(
                cfg => new GtsMotionController(cfg),
                cfg => cfg.Axes.ToDictionary(x => x.Name, x => (int)x.AxisId));

            // 使用源生成器进行JSON反序列化
            _inner.WithJsonDeserializer(json =>
                JsonSerializer.Deserialize(json, MotionJsonContext.Default.GtsMotionControllerConfig)!);

            // 配置组轴映射提取器
            _inner.WithGroupAxesExtractor(cfg =>
                cfg.Groups.ToDictionary(g => g.Name, g => g.Axes.ToArray()));
        }

        public GtsMotionBuilder FromJson(string json)
        {
            _inner.FromJson(json);
            return this;
        }

        public GtsMotionBuilder FromJsonFile(string filePath)
        {
            _inner.FromJsonFile(filePath);
            return this;
        }

        public GtsMotionBuilder AddSingleAxis(string name, ISingleAxis axis)
        {
            _inner.AddSingleAxis(name, axis);
            return this;
        }

        public GtsMotionController BuildController() =>
            (GtsMotionController)_inner.BuildController();

        public MotionRouter BuildRouter() => _inner.BuildRouter();
    }
}
