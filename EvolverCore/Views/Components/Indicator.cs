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
    internal class Indicator : AvaloniaObject, IChartComponentRenderer
    {
        internal List<ChartPlot> ChartPlots { get; } = new List<ChartPlot>();

        ChartComponentViewModel Properties { get; } = new ChartComponentViewModel();

        public void Render(DrawingContext context, ChartPanel chartPanel)
        {
            foreach (ChartPlot plot in ChartPlots) { plot.Render(context, chartPanel); }
        }
    }
}
