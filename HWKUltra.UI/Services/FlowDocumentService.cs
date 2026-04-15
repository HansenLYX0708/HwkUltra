using HWKUltra.Flow.Models;
using HWKUltra.Flow.Utils;

namespace HWKUltra.UI.Services
{
    /// <summary>
    /// Handles Flow JSON load/save operations
    /// </summary>
    public class FlowDocumentService
    {
        /// <summary>
        /// Current file path (null if unsaved new document)
        /// </summary>
        public string? CurrentFilePath { get; private set; }

        /// <summary>
        /// Create a new empty flow definition
        /// </summary>
        public FlowDefinition CreateNew(string name = "New Flow")
        {
            CurrentFilePath = null;
            return new FlowDefinition
            {
                Name = name,
                Description = "New flow definition",
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Load flow definition from JSON file
        /// </summary>
        public FlowDefinition? LoadFromFile(string filePath)
        {
            var definition = FlowSerializer.LoadFromFile(filePath);
            if (definition != null)
                CurrentFilePath = filePath;
            return definition;
        }

        /// <summary>
        /// Save flow definition to current file path
        /// </summary>
        public bool Save(FlowDefinition definition)
        {
            if (string.IsNullOrEmpty(CurrentFilePath))
                return false;

            definition.ModifiedAt = DateTime.Now;
            FlowSerializer.SaveToFile(definition, CurrentFilePath);
            return true;
        }

        /// <summary>
        /// Save flow definition to specified file path
        /// </summary>
        public void SaveAs(FlowDefinition definition, string filePath)
        {
            definition.ModifiedAt = DateTime.Now;
            FlowSerializer.SaveToFile(definition, filePath);
            CurrentFilePath = filePath;
        }
    }
}
