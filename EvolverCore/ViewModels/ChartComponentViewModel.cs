using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{
    public enum CalculationSource
    {
        BarData,
        IndicatorPlot
    }

    internal partial class ChartComponentViewModel : ViewModelBase
    {
        [JsonInclude]
        [ObservableProperty] BarDataSeries? _data = null;
        
        [ObservableProperty] IndicatorViewModel? _sourceIndicator = null;
        [ObservableProperty] int _sourcePlotIndex = -1;

        [ObservableProperty] CalculationSource _source = CalculationSource.BarData;

        [ObservableProperty] string _name = string.Empty;
        [ObservableProperty] string _description = string.Empty;
        [ObservableProperty] int _chartPanelNumber = 0;
        [ObservableProperty] int _renderOrder = 0;

        [ObservableProperty] bool _isHidden;
    }
}
