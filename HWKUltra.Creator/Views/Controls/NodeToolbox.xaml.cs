using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HWKUltra.Creator.Models;

namespace HWKUltra.Creator.Views.Controls
{
    public partial class NodeToolbox : UserControl
    {
        public NodeToolbox()
        {
            InitializeComponent();
        }

        private void NodeEntry_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element)
            {
                var entry = element.Tag as NodeCatalogEntry ?? element.DataContext as NodeCatalogEntry;
                if (entry != null)
                {
                    var data = new DataObject("NodeCatalogEntry", entry);
                    DragDrop.DoDragDrop(element, data, DragDropEffects.Copy);
                }
            }
        }
    }
}
