using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore
{
    public enum PlotStyle
    {
        Line,
        Bar
    }

    internal class ChartPlot
    {
        internal ChartPlot()
        {
            
        }

        internal PlotStyle Style { get; set; } = PlotStyle.Line;

        internal TimeDataSeries Data { get; } = new TimeDataSeries();
    }
}
