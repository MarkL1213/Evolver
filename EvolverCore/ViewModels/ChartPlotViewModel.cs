using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using EvolverCore.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{
    internal partial class ChartPlotViewModel : ViewModelBase
    {
        [ObservableProperty] private BarPointValue _priceField = BarPointValue.Close;

        [ObservableProperty] IBrush? _plotFillBrush = Brushes.Cyan;
        [ObservableProperty] IBrush? _plotLineBrush = Brushes.Turquoise;
        [ObservableProperty] double _plotLineThickness = 1.5;
        [ObservableProperty] IDashStyle? _plotLineStyle = null;
        [ObservableProperty] PlotStyle _style = PlotStyle.Line;
    }
}
