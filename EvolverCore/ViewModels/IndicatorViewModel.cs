using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvolverCore.Models;

namespace EvolverCore.ViewModels
{

    internal partial class IndicatorViewModel : ChartComponentViewModel
    {
        public IndicatorViewModel(Indicator indicator) { _indicator = indicator; }
        [ObservableProperty] Indicator? _indicator;
        
        internal ObservableCollection<ChartPlotViewModel> ChartPlots { get; } = new ObservableCollection<ChartPlotViewModel>();
    }
}
