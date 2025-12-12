using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore
{
    internal class Indicator : ChartComponentBase
    {
        internal List<ChartPlot> ChartPlots { get; } = new List<ChartPlot>();


        public override void Render(DrawingContext context, ChartPanel chartPanel)
        {
            foreach (ChartPlot plot in ChartPlots) { plot.Render(context, chartPanel); }
        }
    }
}
