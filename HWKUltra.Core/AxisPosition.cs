
namespace HWKUltra.Core
{
    public class AxisPosition
    {
        private readonly Dictionary<string, double> _values;

        public AxisPosition(Dictionary<string, double> values)
        {
            _values = values;
        }

        public double this[string axis] => _values[axis];

        public IReadOnlyDictionary<string, double> Values => _values;

        // ===== 运算支持 =====

        public static AxisPosition operator +(AxisPosition a, AxisPosition b)
        {
            return Combine(a, b, (x, y) => x + y);
        }

        public static AxisPosition operator -(AxisPosition a, AxisPosition b)
        {
            return Combine(a, b, (x, y) => x - y);
        }

        private static AxisPosition Combine(
            AxisPosition a,
            AxisPosition b,
            Func<double, double, double> op)
        {
            var result = new Dictionary<string, double>();

            foreach (var axis in a.Values.Keys.Union(b.Values.Keys))
            {
                var v1 = a.Values.ContainsKey(axis) ? a[axis] : 0;
                var v2 = b.Values.ContainsKey(axis) ? b[axis] : 0;

                result[axis] = op(v1, v2);
            }

            return new AxisPosition(result);
        }
    }

    public static class Pos
    {
        // ===== 常用：XY =====
        public static AxisPosition XY(double x, double y)
            => new AxisPosition(new Dictionary<string, double>
            {
                ["X"] = x,
                ["Y"] = y
            });

        // ===== 常用：XYZ =====
        public static AxisPosition XYZ(double x, double y, double z)
            => new AxisPosition(new Dictionary<string, double>
            {
                ["X"] = x,
                ["Y"] = y,
                ["Z"] = z
            });

        // ===== 单轴 =====
        public static AxisPosition X(double x)
            => Create(("X", x));

        public static AxisPosition Y(double y)
            => Create(("Y", y));

        public static AxisPosition Z(double z)
            => Create(("Z", z));

        // ===== 通用构造（最重要）=====
        public static AxisPosition Create(params (string axis, double value)[] items)
        {
            var dict = new Dictionary<string, double>();

            foreach (var (axis, value) in items)
            {
                dict[axis] = value;
            }

            return new AxisPosition(dict);
        }
    }
}
