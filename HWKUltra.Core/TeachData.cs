using System.Text.Json;
using System.Text.Json.Serialization;

namespace HWKUltra.Core
{
    /// <summary>
    /// A single named teach position consisting of axis name-value pairs.
    /// </summary>
    public class TeachPosition
    {
        /// <summary>
        /// Unique name for this position (e.g., "BarcodeScannerPos", "LoadPos")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Group this position belongs to (e.g., "BarcodeScanner", "LoadUnload")
        /// </summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Axis coordinate values (e.g., {"X": 100.0, "Y": 200.0, "Z": 50.0})
        /// </summary>
        public Dictionary<string, double> Axes { get; set; } = new();

        /// <summary>
        /// Convert to AxisPosition for use with MotionRouter
        /// </summary>
        public AxisPosition ToAxisPosition() => new AxisPosition(Axes);

        /// <summary>
        /// Update axes from an AxisPosition
        /// </summary>
        public void FromAxisPosition(AxisPosition pos)
        {
            Axes = new Dictionary<string, double>(pos.Values);
        }

        public override string ToString()
            => $"[{Group}] {Name}: {string.Join(", ", Axes.Select(a => $"{a.Key}={a.Value:F3}"))}";
    }

    /// <summary>
    /// Metadata for a group of teach positions.
    /// </summary>
    public class TeachGroup
    {
        /// <summary>
        /// Group name (e.g., "BarcodeScanner", "Calibration")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Expected axis names for positions in this group (for validation/UI)
        /// </summary>
        public string[] RequiredAxes { get; set; } = [];
    }

    /// <summary>
    /// Root configuration containing all teach groups and positions.
    /// </summary>
    public class TeachDataConfig
    {
        public List<TeachGroup> Groups { get; set; } = new();
        public List<TeachPosition> Positions { get; set; } = new();
    }

    /// <summary>
    /// Service for loading, querying, updating, and saving teach position data.
    /// Thread-safe for read operations; write operations should be called from a single thread.
    /// </summary>
    public class TeachDataService
    {
        private TeachDataConfig _config = new();
        private string? _filePath;
        private readonly object _lock = new();

        /// <summary>
        /// Whether data has been loaded
        /// </summary>
        public bool IsLoaded => _filePath != null;

        /// <summary>
        /// The file path this data was loaded from
        /// </summary>
        public string? FilePath => _filePath;

        /// <summary>
        /// Load teach data from a JSON file
        /// </summary>
        public void Load(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"Teach data file not found: {jsonPath}");

            var json = File.ReadAllText(jsonPath);
            var config = JsonSerializer.Deserialize(json, TeachDataJsonContext.Default.TeachDataConfig);

            lock (_lock)
            {
                _config = config ?? new TeachDataConfig();
                _filePath = jsonPath;
            }
        }

        /// <summary>
        /// Load from a TeachDataConfig object directly (for testing)
        /// </summary>
        public void LoadFrom(TeachDataConfig config, string? filePath = null)
        {
            lock (_lock)
            {
                _config = config;
                _filePath = filePath;
            }
        }

        /// <summary>
        /// Save teach data to the original file path
        /// </summary>
        public void Save(string? path = null)
        {
            var targetPath = path ?? _filePath
                ?? throw new InvalidOperationException("No file path specified and no file was previously loaded.");

            string json;
            lock (_lock)
            {
                json = JsonSerializer.Serialize(_config, TeachDataJsonContext.Default.TeachDataConfig);
            }

            var dir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(targetPath, json);
            _filePath = targetPath;
        }

        // ===== Query =====

        /// <summary>
        /// Get a teach position by name. Returns null if not found.
        /// </summary>
        public TeachPosition? GetPosition(string name)
        {
            lock (_lock)
            {
                return _config.Positions.FirstOrDefault(p =>
                    p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Get a teach position as AxisPosition. Throws if not found.
        /// </summary>
        public AxisPosition GetAxisPosition(string name)
        {
            var pos = GetPosition(name)
                ?? throw new KeyNotFoundException($"Teach position not found: {name}");
            return pos.ToAxisPosition();
        }

        /// <summary>
        /// Try to get a teach position as AxisPosition.
        /// </summary>
        public bool TryGetAxisPosition(string name, out AxisPosition position)
        {
            var tp = GetPosition(name);
            if (tp != null)
            {
                position = tp.ToAxisPosition();
                return true;
            }
            position = Pos.Create();
            return false;
        }

        /// <summary>
        /// Get all positions in a group
        /// </summary>
        public IReadOnlyList<TeachPosition> GetGroup(string groupName)
        {
            lock (_lock)
            {
                return _config.Positions
                    .Where(p => p.Group.Equals(groupName, StringComparison.OrdinalIgnoreCase))
                    .ToList()
                    .AsReadOnly();
            }
        }

        /// <summary>
        /// Get all group names
        /// </summary>
        public IReadOnlyList<string> GetGroupNames()
        {
            lock (_lock)
            {
                return _config.Groups.Select(g => g.Name).ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Get all position names
        /// </summary>
        public IReadOnlyList<string> GetPositionNames()
        {
            lock (_lock)
            {
                return _config.Positions.Select(p => p.Name).ToList().AsReadOnly();
            }
        }

        /// <summary>
        /// Get group metadata
        /// </summary>
        public TeachGroup? GetGroupInfo(string groupName)
        {
            lock (_lock)
            {
                return _config.Groups.FirstOrDefault(g =>
                    g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Get the full config (readonly snapshot)
        /// </summary>
        public TeachDataConfig GetConfig()
        {
            lock (_lock)
            {
                return _config;
            }
        }

        // ===== Modify =====

        /// <summary>
        /// Set (add or update) a teach position
        /// </summary>
        public void SetPosition(string name, string group, AxisPosition pos, string? description = null)
        {
            lock (_lock)
            {
                var existing = _config.Positions.FirstOrDefault(p =>
                    p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.Group = group;
                    existing.Description = description ?? existing.Description;
                    existing.FromAxisPosition(pos);
                }
                else
                {
                    _config.Positions.Add(new TeachPosition
                    {
                        Name = name,
                        Group = group,
                        Description = description,
                        Axes = new Dictionary<string, double>(pos.Values)
                    });
                }

                // Auto-create group if not exists
                if (!_config.Groups.Any(g => g.Name.Equals(group, StringComparison.OrdinalIgnoreCase)))
                {
                    _config.Groups.Add(new TeachGroup { Name = group });
                }
            }
        }

        /// <summary>
        /// Update only the axis values of an existing position
        /// </summary>
        public bool UpdateAxes(string name, AxisPosition pos)
        {
            lock (_lock)
            {
                var existing = _config.Positions.FirstOrDefault(p =>
                    p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (existing == null) return false;
                existing.FromAxisPosition(pos);
                return true;
            }
        }

        /// <summary>
        /// Remove a teach position by name
        /// </summary>
        public bool RemovePosition(string name)
        {
            lock (_lock)
            {
                return _config.Positions.RemoveAll(p =>
                    p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) > 0;
            }
        }

        /// <summary>
        /// Add or update a group definition
        /// </summary>
        public void SetGroup(string name, string? description = null, string[]? requiredAxes = null)
        {
            lock (_lock)
            {
                var existing = _config.Groups.FirstOrDefault(g =>
                    g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.Description = description ?? existing.Description;
                    existing.RequiredAxes = requiredAxes ?? existing.RequiredAxes;
                }
                else
                {
                    _config.Groups.Add(new TeachGroup
                    {
                        Name = name,
                        Description = description,
                        RequiredAxes = requiredAxes ?? []
                    });
                }
            }
        }
    }

    /// <summary>
    /// JSON serialization context for TeachData (AOT-compatible)
    /// </summary>
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(TeachDataConfig))]
    [JsonSerializable(typeof(TeachPosition))]
    [JsonSerializable(typeof(TeachGroup))]
    [JsonSerializable(typeof(List<TeachPosition>))]
    [JsonSerializable(typeof(List<TeachGroup>))]
    [JsonSerializable(typeof(Dictionary<string, double>))]
    public partial class TeachDataJsonContext : JsonSerializerContext
    {
    }
}
