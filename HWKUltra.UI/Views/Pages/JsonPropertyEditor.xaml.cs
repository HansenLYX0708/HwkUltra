using System.Windows;
using System.Windows.Controls;
using HWKUltra.UI.Models;

namespace HWKUltra.UI.Views.Pages
{
    public partial class JsonPropertyEditor : UserControl
    {
        public JsonPropertyEditor()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// Whether the current node has children (Object or Array)
        /// </summary>
        public static readonly DependencyProperty HasChildrenProperty =
            DependencyProperty.Register(nameof(HasChildren), typeof(Visibility), typeof(JsonPropertyEditor),
                new PropertyMetadata(Visibility.Collapsed));

        public Visibility HasChildren
        {
            get => (Visibility)GetValue(HasChildrenProperty);
            set => SetValue(HasChildrenProperty, value);
        }

        /// <summary>
        /// Whether this is an array (for showing Add button)
        /// </summary>
        public static readonly DependencyProperty IsArrayProperty =
            DependencyProperty.Register(nameof(IsArray), typeof(Visibility), typeof(JsonPropertyEditor),
                new PropertyMetadata(Visibility.Collapsed));

        public Visibility IsArray
        {
            get => (Visibility)GetValue(IsArrayProperty);
            set => SetValue(IsArrayProperty, value);
        }

        /// <summary>
        /// Whether this item is an array element (for showing Remove button)
        /// </summary>
        public static readonly DependencyProperty IsArrayItemProperty =
            DependencyProperty.Register(nameof(IsArrayItem), typeof(Visibility), typeof(JsonPropertyEditor),
                new PropertyMetadata(Visibility.Collapsed));

        public Visibility IsArrayItem
        {
            get => (Visibility)GetValue(IsArrayItemProperty);
            set => SetValue(IsArrayItemProperty, value);
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is JsonPropertyModel model)
            {
                var isContainer = model.PropType == JsonPropType.Object || model.PropType == JsonPropType.Array;
                HasChildren = isContainer ? Visibility.Visible : Visibility.Collapsed;
                IsArray = model.PropType == JsonPropType.Array ? Visibility.Visible : Visibility.Collapsed;
                IsArrayItem = model.ArrayIndex >= 0 ? Visibility.Visible : Visibility.Collapsed;

                // Hide leaf editor for container types
                LeafGrid.Visibility = isContainer ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }
}
