using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{
    internal partial class ChartPlotViewModel : ViewModelBase
    {
        [ObservableProperty] internal IBrush? _plotFillBrush = Brushes.Cyan;
        [ObservableProperty] internal IBrush? _plotLineBrush = Brushes.Turquoise;
        [ObservableProperty] internal double _plotLineThickness = 1.5;
        [ObservableProperty] internal IDashStyle? _plotLineStyle = null;
    }
}
