using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Avalonia.Media;

namespace EvolverCore.ViewModels.Indicators
{
    internal partial class VolumeViewModel : IndicatorViewModel
    {
        [ObservableProperty] IBrush _bullBrush = Brushes.DodgerBlue;
        [ObservableProperty] IBrush _bearBrush = Brushes.Red;

        public VolumeViewModel(IndicatorDataSlice barSeries)
        {
            Name = "Volume";
            Data = barSeries;
        }


    }
}