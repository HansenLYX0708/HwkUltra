using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HWKUltra.Creator.Models;
using HWKUltra.Creator.Services;

namespace HWKUltra.Creator.ViewModels
{
    /// <summary>
    /// ViewModel for the node toolbox (categorized list of available nodes)
    /// </summary>
    public partial class NodeToolboxViewModel : ObservableObject
    {
        private readonly NodeCatalogService _catalogService;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private NodeCatalogEntry? _selectedEntry;

        public ObservableCollection<NodeCatalogCategory> Categories { get; } = new();
        public ObservableCollection<NodeCatalogCategory> FilteredCategories { get; } = new();

        public NodeToolboxViewModel(NodeCatalogService catalogService)
        {
            _catalogService = catalogService;
            LoadCategories();
        }

        private void LoadCategories()
        {
            Categories.Clear();
            FilteredCategories.Clear();

            foreach (var category in _catalogService.GetCategories())
            {
                Categories.Add(category);
                FilteredCategories.Add(category);
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilteredCategories.Clear();

            if (string.IsNullOrWhiteSpace(value))
            {
                foreach (var cat in Categories)
                    FilteredCategories.Add(cat);
                return;
            }

            var search = value.Trim().ToLowerInvariant();
            foreach (var cat in Categories)
            {
                var filteredEntries = cat.Entries
                    .Where(e => e.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || e.Type.Contains(search, StringComparison.OrdinalIgnoreCase)
                             || e.Category.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (filteredEntries.Count > 0)
                {
                    FilteredCategories.Add(new NodeCatalogCategory
                    {
                        Name = cat.Name,
                        Color = cat.Color,
                        Entries = filteredEntries
                    });
                }
            }
        }
    }
}
