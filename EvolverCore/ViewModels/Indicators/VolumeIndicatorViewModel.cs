using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Avalonia.Media;

namespace EvolverCore.ViewModels
{
    internal partial class VolumeIndicatorViewModel : IndicatorViewModel
    {
        [ObservableProperty] IBrush _bullBrush = Brushes.DodgerBlue;
        [ObservableProperty] IBrush _bearBrush = Brushes.Red;

        public VolumeIndicatorViewModel(BarDataSeries barSeries)
        {
            Name = "Volume";
            Data = barSeries;
        }


    }
}