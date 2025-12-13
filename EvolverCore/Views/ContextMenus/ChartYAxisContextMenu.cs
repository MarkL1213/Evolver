using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
