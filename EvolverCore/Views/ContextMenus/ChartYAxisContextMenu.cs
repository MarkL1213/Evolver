using Avalonia.Controls;

namespace EvolverCore.Views.ContextMenus
{
    internal static class ChartYAxisContextMenu
    {
        public static ContextMenu CreateDefault()
        {
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.Items.Add(new MenuItem() { Header = "YAxis Test 1" });
            return contextMenu;
        }
    }
}
