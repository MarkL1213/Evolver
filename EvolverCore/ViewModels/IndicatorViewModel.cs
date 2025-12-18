using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{
    public enum IndicatorState
    {
        History,
        Live
    }
    internal partial class IndicatorViewModel : ChartComponentViewModel
    {
        [ObservableProperty] IndicatorState _state = IndicatorState.History;
        internal ObservableCollection<ChartPlotViewModel> ChartPlots { get; } = new ObservableCollection<ChartPlotViewModel>();
    }
}
