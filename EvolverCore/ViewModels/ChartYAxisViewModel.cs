using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{
    internal partial class ChartYAxisViewModel : ViewModelBase
    {
        public static IBrush DefaultLabelColor = Brushes.White;
        public static IBrush DefaultBackgroundColor = Brushes.Black;
        public static int DefaultFontSize = 12;

        public static IBrush DefaultTickLineColor = Brushes.White;
        public static double DefaultTickLineThickness = 1;
        public static IDashStyle? DefaultTickLineDashStyle = null;

        [ObservableProperty] double _min = 0;
        [ObservableProperty] double _max = 100;

        [ObservableProperty] IBrush _backgroundColor = DefaultBackgroundColor;
        [ObservableProperty] IBrush _labelColor = DefaultLabelColor;
        [ObservableProperty] int _fontSize = DefaultFontSize;

        [ObservableProperty] IBrush _tickLineColor = DefaultTickLineColor;
        [ObservableProperty] double _tickLineThickness = DefaultTickLineThickness;
        [ObservableProperty] IDashStyle? _tickLineDashStyle = DefaultTickLineDashStyle;
    }
}
