using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using EvolverCore.ViewModels;
using EvolverCore.Views.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using static EvolverCore.ChartControl;

namespace EvolverCore.Views.Components
{
    public class VolumeIndicator : Indicator
    {
        public VolumeIndicator(ChartPanel panel) : base(panel)
        {
        }

        public override void ConfigurePlots()
        {
            ChartPlotViewModel plotProperties = new ChartPlotViewModel();
            plotProperties.Component = Properties;
            plotProperties.PriceField = BarPointValue.Volume;
            plotProperties.Style = PlotStyle.Bar;

            ChartPlot plot = new ChartPlot(this) { Properties = plotProperties };
            AddPlot(plot);
        }

    }
}