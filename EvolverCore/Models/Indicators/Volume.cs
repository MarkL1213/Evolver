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
    public class Volume : Indicator
    {
        public Volume()
        {
            Name = "Volume";
        }

        public override void Configure()
        {
            PlotProperties plotProperties = new PlotProperties();
            plotProperties.Style = PlotStyle.Bar;
            OutputPlot oplot = new OutputPlot(plotProperties);
            Outputs.Add(oplot);
        }

        public void Calculate()
        {
            if (Bars.Count <= 0) return;
            OutputPlot oPLot = Outputs[0];
            if (oPLot.Series == null) return;

            for (int i = 0; i < Bars[0].Count; i++)
            {
                TimeDataBar bar = Bars[0].GetValueAt(i);
                oPLot.Series.Add(new TimeDataPoint(bar.Time, bar.Volume));
            }
        }

        public override void OnDataUpdate()
        {
            Outputs[0][0] = Bars[0][0].Volume;
        }

    }
}