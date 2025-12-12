using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using EvolverCore.Data;

namespace EvolverCore.ViewModels
{
    internal partial class ChartControlViewModel : ViewModelBase
    {
        public ChartXAxisViewModel? SharedXAxis { get; } = new ChartXAxisViewModel()
        {
            Min = DateTime.Now.AddHours(-12),
            Max = DateTime.Now
        };

        public ChartPanelViewModel PrimaryChartPanelViewModel { get; } = new ChartPanelViewModel();

        public int MainBorderThickness { get; } = 3;
        public IBrush MainBorderColor { get; } = Brushes.Blue;

        public ObservableCollection<ChartPanelViewModel> SubPanelViewModels { get; } = new ObservableCollection<ChartPanelViewModel>();

        public ChartControlViewModel()
        {
            PrimaryChartPanelViewModel.XAxis = SharedXAxis;
        }
    }
}
