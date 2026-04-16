using System.Text.Json;
using System.Text.Json.Serialization;

namespace HWKUltra.Core
{
    public class AxisPosition
    {
        private readonly Dictionary<string, double> _values;

        public AxisPosition(Dictionary<string, double> values)
        {
            _values = new Dictionary<string, double>(values);
        }

        public double this[string axis] => _values[axis];

        public IReadOnlyDictionary<string, double> Values => _values;

        /// <summary>
        /// Check if a named axis exists in this position
        /// </summary>
        public bool HasAxis(string axis) => _values.ContainsKey(axis);

        /// <summary>
        /// Try to get the value for a named axis
        /// </summary>
        public bool TryGetValue(string axis, out double value) => _values.TryGetValue(axis, out value);

        /// <summary>
        /// Get value for axis, or default if not present
        /// </summary>
        public double GetValueOrDefault(string axis, double defaultValue = 0.0)
            => _values.TryGetValue(axis, out var v) ? v : defaultValue;

        /// <summary>
        /// Number of axes in this position
        /// </summary>
        public int Count => _values.Count;

        /// <summary>
        /// Extract X, Y, Z values as a tuple (defaults to 0 if axis is missing)
        /// </summary>
        public (double X, double Y, double Z) ToXYZ()
            => (GetValueOrDefault("X"), GetValueOrDefault("Y"), GetValueOrDefault("Z"));

        /// <summary>
        /// Create an AxisPosition from X, Y, Z values
        /// </summary>
        public static AxisPosition FromXYZ(double x, double y, double z)
            => Pos.XYZ(x, y, z);

        // ===== Arithmetic Operators =====

        public static AxisPosition operator +(AxisPosition a, AxisPosition b)
        {
            return Combine(a, b, (x, y) => x + y);
        }

        public static AxisPosition operator -(AxisPosition a, AxisPosition b)
        {
            return Combine(a, b, (x, y) => x - y);
        }

        public static AxisPosition operator *(AxisPosition a, double scalar)
        {
            var result = new Dictionary<string, double>();
            foreach (var kvp in a.Values)
                result[kvp.Key] = kvp.Value * scalar;
            return new AxisPosition(result);
        }

        public override string ToString()
        {
            var parts = _values.Select(kvp => $"{kvp.Key}={kvp.Value:F3}");
            return $"({string.Join(", ", parts)})";
        }

        /// <summary>
        /// Serialize to JSON string
        /// </summary>
        public string ToJson()
            => JsonSerializer.Serialize(_values, CoreJsonContext.Default.DictionaryStringDouble);

        /// <summary>
        /// Deserialize from JSON string (dictionary format)
        /// </summary>
        public static AxisPosition FromJson(string json)
        {
            var dict = JsonSerializer.Deserialize(json, CoreJsonContext.Default.DictionaryStringDouble)
                       ?? new Dictionary<string, double>();
            return new AxisPosition(dict);
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
