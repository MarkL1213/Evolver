using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using EvolverCore.ViewModels;
using EvolverCore.ViewModels.Indicators;
using EvolverCore.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using static EvolverCore.ChartControl;

namespace EvolverCore.Models.Indicators
{
    public class Volume : IndicatorComponent
    {
        public Volume(ChartPanel panel) : base(panel)
        {
        }

        public override void ConfigurePlots()
        {
            ChartPlotViewModel plotProperties = new ChartPlotViewModel();
            plotProperties.Component = Properties;
            plotProperties.PriceField = BarPriceValue.Volume;
            plotProperties.Style = PlotStyle.Bar;
            plotProperties.Name = "Volume";

            ChartPlot plot = new ChartPlot(this) { Properties = plotProperties };
            AddPlot(plot);
        }

        //public override void Calculate()
        //{
        //    VolumeViewModel? viVM = Properties as VolumeViewModel;
        //    if (viVM == null) return;

        //    BarDataSeries? inputSeries = viVM.Data;
        //    if (inputSeries == null) return;

        //    TimeDataSeries outputSeries = viVM.ChartPlots[0].PlotSeries;
        //    foreach (TimeDataBar bar in inputSeries)
        //    {
        //        outputSeries.Add(new TimeDataPoint(bar.Time, bar.Volume));
        //    }
        //}

    }
}