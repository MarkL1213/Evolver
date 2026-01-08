using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using EvolverCore.ViewModels;
using EvolverCore.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using static EvolverCore.ChartControl;

namespace EvolverCore.Models.Indicators
{
    public class Volume : Indicator
    {
        public Volume(IndicatorProperties properties) : base(properties)
        {
            Name = "Volume";
        }

        public override void Configure()
        {
            PlotProperties plotProperties = new PlotProperties();
            OutputPlot oplot = new OutputPlot("Volume", plotProperties, PlotStyle.Bar);
            Outputs.Add(oplot);
        }

        public override void OnDataUpdate()
        {
            if (CurrentBarIndex % 5 == 0)
            {
                Outputs[0].Properties[0].PlotFillBrush = Brushes.Orange;
            }

            Outputs[0][0] = Bars[0][0].Volume;
        }

    }
}