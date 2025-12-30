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

        [ObservableProperty] IndicatorDataSlice? _data = null;
        
        [ObservableProperty] string _name = string.Empty;
        [ObservableProperty] string _description = string.Empty;
        [ObservableProperty] int _chartPanelNumber = 0;
        [ObservableProperty] int _renderOrder = 0;

        [ObservableProperty] bool _isHidden;
    }
}
