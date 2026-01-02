using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Avalonia.Media;
using EvolverCore.Models;

namespace EvolverCore.ViewModels.Indicators
{
    internal partial class VolumeViewModel : IndicatorViewModel
    {
        [ObservableProperty] IBrush _bullBrush = Brushes.DodgerBlue;
        [ObservableProperty] IBrush _bearBrush = Brushes.Red;

        public VolumeViewModel(Indicator indicator)
        {
            Name = "Volume";
            Indicator = indicator;
        }


    }
}