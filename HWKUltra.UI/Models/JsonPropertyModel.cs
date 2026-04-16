using System.Collections.ObjectModel;
using System.Text.Json;

namespace HWKUltra.UI.Models
{
    /// <summary>
    /// Represents the inferred JSON value type for dynamic property editing
    /// </summary>
    public enum JsonPropType
    {
        String,
        Number,
        Boolean,
        Object,
        Array,
        Null
    }

    /// <summary>
    /// ViewModel for a single JSON property node, supporting recursive nesting.
    /// Drives the dynamic property editor UI — fields are never hardcoded.
    /// </summary>
    public partial class JsonPropertyModel : ObservableObject
    {
        [ObservableProperty]
        private string _key = string.Empty;

        [ObservableProperty]
        private string _value = string.Empty;

        [ObservableProperty]
        private JsonPropType _propType = JsonPropType.String;

        [ObservableProperty]
        private bool _isExpanded = true;

        /// <summary>
        /// Child properties (for Object / Array types)
        /// </summary>
        public ObservableCollection<JsonPropertyModel> Children { get; } = new();

        /// <summary>
        /// Index within parent array (-1 if not an array element)
        /// </summary>
        public int ArrayIndex { get; set; } = -1;

        /// <summary>
        /// Display label: "Key" for objects, "[N]" for array elements
        /// </summary>
        public string DisplayKey => ArrayIndex >= 0 ? $"[{ArrayIndex}]" : Key;

        /// <summary>
        /// Parse a JsonElement into a tree of JsonPropertyModel nodes
        /// </summary>
        public static ObservableCollection<JsonPropertyModel> FromJsonElement(JsonElement element)
        {
            var result = new ObservableCollection<JsonPropertyModel>();

            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    result.Add(CreateFromProperty(prop.Name, prop.Value, -1));
                }
            }
            return result;
        }

        /// <summary>
        /// Serialize the property tree back to a JsonElement (via writer)
        /// </summary>
        public static void WriteToJson(Utf8JsonWriter writer, ObservableCollection<JsonPropertyModel> properties)
        {
            writer.WriteStartObject();
            foreach (var prop in properties)
            {
                WritePropertyToJson(writer, prop);
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// Serialize entire tree to formatted JSON string
        /// </summary>
        public static string ToJsonString(ObservableCollection<JsonPropertyModel> properties)
        {
            using var ms = new System.IO.MemoryStream();
            using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
            WriteToJson(writer, properties);
            writer.Flush();
            return System.Text.Encoding.UTF8.GetString(ms.ToArray());
        }

        private static JsonPropertyModel CreateFromProperty(string key, JsonElement value, int arrayIndex)
        {
            var model = new JsonPropertyModel
            {
                Key = key,
                ArrayIndex = arrayIndex
            };

            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    model.PropType = JsonPropType.String;
                    model.Value = value.GetString() ?? string.Empty;
                    break;

                case JsonValueKind.Number:
                    model.PropType = JsonPropType.Number;
                    model.Value = value.GetRawText();
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    model.PropType = JsonPropType.Boolean;
                    model.Value = value.GetBoolean().ToString();
                    break;

                case JsonValueKind.Null:
                    model.PropType = JsonPropType.Null;
                    model.Value = string.Empty;
                    break;

                case JsonValueKind.Object:
                    model.PropType = JsonPropType.Object;
                    foreach (var child in value.EnumerateObject())
                        model.Children.Add(CreateFromProperty(child.Name, child.Value, -1));
                    break;

                case JsonValueKind.Array:
                    model.PropType = JsonPropType.Array;
                    int idx = 0;
                    foreach (var item in value.EnumerateArray())
                    {
                        model.Children.Add(CreateFromProperty(string.Empty, item, idx));
                        idx++;
                    }
                    break;
            }

            return model;
        }

        private static void WritePropertyToJson(Utf8JsonWriter writer, JsonPropertyModel prop)
        {
            // Write property name (skip for array elements — parent writes array start)
            if (prop.ArrayIndex < 0 && !string.IsNullOrEmpty(prop.Key))
                writer.WritePropertyName(prop.Key);

            switch (prop.PropType)
            {
                case JsonPropType.String:
                    writer.WriteStringValue(prop.Value);
                    break;

                case JsonPropType.Number:
                    if (double.TryParse(prop.Value, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var num))
                    {
                        // Preserve integer vs decimal
                        if (num == Math.Floor(num) && !prop.Value.Contains('.'))
                            writer.WriteNumberValue((long)num);
                        else
                            writer.WriteNumberValue(num);
                    }
                    else
                    {
                        writer.WriteNumberValue(0);
                    }
                    break;

                case JsonPropType.Boolean:
                    writer.WriteBooleanValue(
                        prop.Value.Equals("True", StringComparison.OrdinalIgnoreCase));
                    break;

                case JsonPropType.Null:
                    writer.WriteNullValue();
                    break;

                case JsonPropType.Object:
                    writer.WriteStartObject();
                    foreach (var child in prop.Children)
                        WritePropertyToJson(writer, child);
                    writer.WriteEndObject();
                    break;

                case JsonPropType.Array:
                    writer.WriteStartArray();
                    foreach (var child in prop.Children)
                        WritePropertyToJson(writer, child);
                    writer.WriteEndArray();
                    break;
            }
        }

        /// <summary>
        /// Create a deep clone of this property (for adding new array items from template)
        /// </summary>
        public JsonPropertyModel DeepClone()
        {
            var clone = new JsonPropertyModel
            {
                Key = Key,
                Value = Value,
                PropType = PropType,
                ArrayIndex = ArrayIndex,
                IsExpanded = IsExpanded
            };
            foreach (var child in Children)
                clone.Children.Add(child.DeepClone());
            return clone;
        }
    }
}
