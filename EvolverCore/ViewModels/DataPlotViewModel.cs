using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EvolverCore.ViewModels
{
    internal partial class DataPlotViewModel : ChartPlotViewModel
    {
        public static double DefaultPreferredCandleWidth = 5;
        public static IBrush DefaultCandleUpColor = Brushes.DodgerBlue;
        public static IBrush DefaultCandleDownColor = Brushes.Red;

        public static IBrush DefaultWickColor = Brushes.DarkGray;
        public static double DefaultWickThickness = 1;
        public static IDashStyle? DefaultWickDashStyle = null;

        public static IBrush DefaultCandleOutlineColor = Brushes.DarkGray;
        public static double DefaultCandleOutlineThickness = 1;
        public static IDashStyle? DefaultCandleOutlineDashStyle = null;

        [ObservableProperty] private double _preferredCandleWidth = DefaultPreferredCandleWidth;

        [ObservableProperty] IBrush _candleUpColor = DefaultCandleUpColor;
        [ObservableProperty] IBrush _candleDownColor = DefaultCandleDownColor;

        [ObservableProperty] IBrush _candleOutlineColor = DefaultCandleOutlineColor;
        [ObservableProperty] double _candleOutlineThickness = DefaultCandleOutlineThickness;
        [ObservableProperty] IDashStyle? _candleOutlineDashStyle = DefaultCandleOutlineDashStyle;

        [ObservableProperty] IBrush _wickColor = DefaultWickColor;
        [ObservableProperty] double _wickThickness = DefaultWickThickness;
        [ObservableProperty] IDashStyle? _wickDashStyle = DefaultWickDashStyle;
    }
}
