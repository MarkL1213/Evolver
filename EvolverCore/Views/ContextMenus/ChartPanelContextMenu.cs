using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Views.ContextMenus
{
    internal static class ChartPanelContextMenu
    {
        public static ContextMenu CreateDefault()
        {
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.Items.Add(new MenuItem() { Header = "Panel Test 1" });
            return contextMenu;
        }
    }
}
