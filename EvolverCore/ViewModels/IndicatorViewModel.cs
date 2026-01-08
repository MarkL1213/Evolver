using CommunityToolkit.Mvvm.ComponentModel;
using EvolverCore.Models;
using System.Collections.ObjectModel;

namespace EvolverCore.ViewModels
{

    internal partial class IndicatorViewModel : ChartComponentViewModel
    {
        public IndicatorViewModel(Indicator indicator) { _indicator = indicator; }
        [ObservableProperty] Indicator? _indicator;

        internal ObservableCollection<ChartPlotViewModel> ChartPlots { get; } = new ObservableCollection<ChartPlotViewModel>();
    }
}
