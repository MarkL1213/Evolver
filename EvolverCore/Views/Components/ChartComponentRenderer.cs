using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Views.Components
{
    internal interface IChartComponentRenderer
    {
        public int RenderOrder { get; set; }

        public void Render(DrawingContext context, ChartPanel chartPanel);
    }
}
