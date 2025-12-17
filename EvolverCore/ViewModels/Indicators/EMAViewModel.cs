using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels.Indicators
{
    internal partial class EMAViewModel : IndicatorViewModel
    {
        [ObservableProperty] int _period = 14;
        [ObservableProperty] int _smoothing = 2;

        public EMAViewModel(IndicatorViewModel source, int period, int smoothing, int sourcePlotIndex)
        {
            Name = "EMA";
            Source = CalculationSource.IndicatorPlot;
            SourcePlotIndex = sourcePlotIndex;
            _period = period;
            _smoothing = smoothing;
            SourceIndicator = source;
        }

        public EMAViewModel(BarDataSeries barSeries, int period, int smoothing)
        {
            Name = "EMA";
            Source = CalculationSource.BarData;
            SourcePlotIndex = -1;
            _period = period;
            _smoothing = smoothing;
            Data = barSeries;
        }
    }
}
