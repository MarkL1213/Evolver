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
    internal class Indicator : ChartComponentBase
    {
        internal Indicator(ChartPanel parent):base(parent) { }
        internal List<ChartPlot> ChartPlots { get; } = new List<ChartPlot>();

        internal void AddPlot(ChartPlot plot)
        {
            IndicatorViewModel? vm = Properties as IndicatorViewModel;
            if (vm == null) return;
            ChartPlots.Add(plot);
            vm.ChartPlots.Add(plot.Properties);
        }

        public override void Render(DrawingContext context)
        {
            foreach (ChartPlot plot in ChartPlots) { plot.Render(context); }
        }
    }
}
