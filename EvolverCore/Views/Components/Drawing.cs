using Avalonia;
using Avalonia.Media;
using EvolverCore.ViewModels;
using EvolverCore.Views.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Views
{
    internal class Drawing : AvaloniaObject, IChartComponentRenderer
    {
        ChartComponentViewModel Properties { get; } = new ChartComponentViewModel();
        public void Render(DrawingContext context, ChartPanel panel)
        {
        }
    }
}
