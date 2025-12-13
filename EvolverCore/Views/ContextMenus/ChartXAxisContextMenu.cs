using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
