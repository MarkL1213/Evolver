using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using EvolverCore.Models;
using EvolverCore.Views.Components;
using System;
using System.Collections.ObjectModel;


namespace EvolverCore.ViewModels
{
    public partial class ChartControlViewModel : ViewModelBase
    {
        public ChartXAxisViewModel? SharedXAxis { get; } = new ChartXAxisViewModel()
        {
            Min = DateTime.Now.AddHours(-12),
            Max = DateTime.Now
        };

        public ChartPanelViewModel PrimaryChartPanelViewModel { get; } = new ChartPanelViewModel();

        [ObservableProperty] Thickness _mainBorderThickness = new Thickness(3);

        [ObservableProperty]
        SerializableBrush _mainBorderColor = new SerializableBrush(Brushes.Blue);

        [ObservableProperty] string _name = string.Empty;

        public ObservableCollection<SubPanel> SubPanelViewModels { get; } = new ObservableCollection<SubPanel>();

        public ChartControlViewModel()
        {
            PrimaryChartPanelViewModel.XAxis = SharedXAxis;

        }
    }
}
