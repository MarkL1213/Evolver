using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{
    internal partial class IndicatorViewModel : ChartComponentViewModel
    {
        internal ObservableCollection<ChartPlotViewModel> ChartPlots { get; } = new ObservableCollection<ChartPlotViewModel>();
    }
}
