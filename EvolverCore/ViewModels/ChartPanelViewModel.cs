using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;

namespace EvolverCore.ViewModels
{
    internal partial class ChartPanelViewModel : ViewModelBase
    {
        
        public static double DefaultScrollSensitivity = .25;
        public static double DefaultPanSensitivity = 1;
        public static bool DefaultShowGridLines = true;
        public static bool DefaultShowCrosshair = true;


        public static IBrush DefaultGridLinesColor = Brushes.DarkGray;
        public static double DefaultGridLinesThickness = 1;
        public static IDashStyle DefaultGridLinesDashStyle = new ImmutableDashStyle(new double[] { 1, 1 }, 0);

        public static IBrush DefaultGridLinesBoldColor = Brushes.Gray;
        public static double DefaultGridLinesBoldThickness = 2;
        public static IDashStyle? DefaultGridLinesBoldDashStyle = null;

        public static CrosshairSnapMode DefaultCrosshaitSnapMode = CrosshairSnapMode.Free;
        public static IBrush DefaultCrosshairLineColor = Brushes.Gray;
        public static double DefaultCrosshairLineThickness = 1;
        public static IDashStyle? DefaultCrosshairLineDashStyle = null;
        public static IBrush DefaultCrosshairReadoutForegroundColor = Brushes.White;
        public static IBrush DefaultCrosshairReadoutBackgroundColor = Brushes.Black;
        public static int DefaultCrosshairReadoutFontSize = 12;

        public static IBrush DefaultBackgroundColor = Brushes.Black;
        
        public static IBrush DefaultCandleUpColor = Brushes.DodgerBlue;
        public static IBrush DefaultCandleDownColor = Brushes.Red;

        public static IBrush DefaultWickColor = Brushes.DarkGray;
        public static double DefaultWickThickness = 1;
        public static IDashStyle? DefaultWickDashStyle = null;

        public static IBrush DefaultCandleOutlineColor = Brushes.DarkGray;
        public static double DefaultCandleOutlineThickness = 1;
        public static IDashStyle? DefaultCandleOutlineDashStyle = null;

        internal ChartXAxisViewModel? XAxis { set; get; }
        internal ChartYAxisViewModel YAxis { get; } = new ChartYAxisViewModel();
        
        internal ObservableCollection<ChartComponentViewModel> ChartComponents { get; } = new ObservableCollection<ChartComponentViewModel>();

                [ObservableProperty] double _scrollSensitivity = DefaultScrollSensitivity;
        [ObservableProperty] double _panSensitivity = DefaultPanSensitivity;
        [ObservableProperty] bool _showGridLines = DefaultShowGridLines;
        [ObservableProperty] bool _showCrosshair = DefaultShowCrosshair;

        [ObservableProperty] IBrush _backgroundColor = DefaultBackgroundColor;

        [ObservableProperty] IBrush _candleUpColor = DefaultCandleUpColor;
        [ObservableProperty] IBrush _candleDownColor = DefaultCandleDownColor;

        [ObservableProperty] IBrush _candleOutlineColor = DefaultCandleOutlineColor;
        [ObservableProperty] double _candleOutlineThickness = DefaultCandleOutlineThickness;
        [ObservableProperty] IDashStyle? _candleOutlineDashStyle = DefaultCandleOutlineDashStyle;

        [ObservableProperty] IBrush _wickColor = DefaultWickColor;
        [ObservableProperty] double _wickThickness = DefaultWickThickness;
        [ObservableProperty] IDashStyle? _wickDashStyle = DefaultWickDashStyle;

        [ObservableProperty] IBrush _gridLinesColor = DefaultGridLinesColor;
        [ObservableProperty] double _gridLinesThickness = DefaultGridLinesThickness;
        [ObservableProperty] IDashStyle? _gridLinesDashStyle = DefaultGridLinesDashStyle;

        [ObservableProperty] IBrush _gridLinesBoldColor = DefaultGridLinesBoldColor;
        [ObservableProperty] double _gridLinesBoldThickness = DefaultGridLinesBoldThickness;
        [ObservableProperty] IDashStyle? _gridLinesBoldDashStyle = DefaultGridLinesBoldDashStyle;

        [ObservableProperty] IBrush _crosshairLineColor = DefaultCrosshairLineColor;
        [ObservableProperty] double _crosshairLineThickness = DefaultCrosshairLineThickness;
        [ObservableProperty] IDashStyle? _crosshairLineDashStyle = DefaultCrosshairLineDashStyle;

        [ObservableProperty] IBrush _crosshairReadoutForegroundColor = DefaultCrosshairReadoutForegroundColor;
        [ObservableProperty] IBrush _crosshairReadoutBackgroundColor = DefaultCrosshairReadoutBackgroundColor;
        [ObservableProperty] int _crosshairReadoutFontSize = DefaultCrosshairReadoutFontSize;

        [ObservableProperty] CrosshairSnapMode _crosshairSnapMode = DefaultCrosshaitSnapMode;

        [ObservableProperty] private Point? _mousePosition = null;
        [ObservableProperty] private DateTime? _crosshairTime = null;
        [ObservableProperty] private double? _crosshairPrice = null;
    }

}
