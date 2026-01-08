using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using EvolverCore.Models;
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
        [ObservableProperty] IndicatorViewModel? _indicator;

        [ObservableProperty] int _plotIndex = -1;

        [ObservableProperty] BarPriceValue _priceField = BarPriceValue.Close;

        [ObservableProperty] SerializableBrush _plotFillBrush = new SerializableBrush(Brushes.Cyan);
        [ObservableProperty] SerializableBrush _plotLineBrush = new SerializableBrush(Brushes.Turquoise);
        [ObservableProperty] double _plotLineThickness = 1.5;
        [ObservableProperty] SerializableDashStyle _plotLineStyle = new SerializableDashStyle();
        [ObservableProperty] PlotStyle _style = PlotStyle.Line;
    }
}
