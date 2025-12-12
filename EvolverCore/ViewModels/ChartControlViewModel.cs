using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

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

        [ObservableProperty] Thickness _mainBorderThickness = new Thickness(3);
        [ObservableProperty] IBrush _mainBorderColor = Brushes.Blue;

        public ObservableCollection<ChartControl.SubPanel> SubPanelViewModels { get; } = new ObservableCollection<ChartControl.SubPanel>();

        public ChartControlViewModel()
        {
            PrimaryChartPanelViewModel.XAxis = SharedXAxis;
        }
    }
}
