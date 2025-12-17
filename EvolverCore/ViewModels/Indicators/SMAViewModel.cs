using CommunityToolkit.Mvvm.ComponentModel;
using EvolverCore.Views.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels.Indicators
{
    internal partial class SMAViewModel : IndicatorViewModel
    {
        [ObservableProperty] int _period = 14;

        public SMAViewModel(IndicatorViewModel source, int period, int sourcePlotIndex)
        {
            Name = "SMA";
            Source = CalculationSource.IndicatorPlot;
            SourcePlotIndex = sourcePlotIndex;
            _period = period;
            SourceIndicator = source;
        }

        public SMAViewModel(BarDataSeries barSeries, int period)
        {
            Name = "SMA";
            Source = CalculationSource.BarData;
            SourcePlotIndex = -1;
            _period = period;
            Data = barSeries;
        }
    }
}
