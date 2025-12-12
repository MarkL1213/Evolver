using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using EvolverCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{
    internal partial class ChartPanelViewModel : ViewModelBase
    {
        public static IBrush DefaultGridLinesColor = Brushes.DarkGray;
        public static double DefaultGridLinesThickness = 1;
        public static IDashStyle DefaultGridLinesDashStyle = new ImmutableDashStyle(new double[] { 1, 1 }, 0);

        public static IBrush DefaultGridLinesBoldColor = Brushes.Gray;
        public static double DefaultGridLinesBoldThickness = 2;
        public static IDashStyle? DefaultGridLinesBoldDashStyle = null;

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
        
        internal ObservableCollection<BarDataSeries> Data { get; } = new ObservableCollection<BarDataSeries>();
        internal ObservableCollection<ChartComponentBase> ChartComponents { get; } = new ObservableCollection<ChartComponentBase>();


        [ObservableProperty] bool _showGridLines = true;
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

        //[ObservableProperty] ChartYAxis? _connectedChartYAxis = null;
    }
}
