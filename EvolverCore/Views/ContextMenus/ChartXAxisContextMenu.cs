using Avalonia.Controls;

namespace EvolverCore.Views.ContextMenus
{
    internal static class ChartXAxisContextMenu
    {
        public static ContextMenu CreateDefault()
        {
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.Items.Add(new MenuItem() { Header = "XAxis Test 1" });
            return contextMenu;
        }
    }
}
