using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvolverCore.Data;

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

        internal ChartXAxisViewModel? XAxis { set; get; }
        internal ChartYAxisViewModel YAxis { get; } = new ChartYAxisViewModel();
        internal IDataSeries<IDataPoint> Data { get; set; }



        [ObservableProperty] bool _showGridLines = true;
        [ObservableProperty] IBrush _backgroundColor = DefaultBackgroundColor;

        [ObservableProperty] IBrush _gridLinesColor = DefaultGridLinesColor;
        [ObservableProperty] double _gridLinesThickness = DefaultGridLinesThickness;
        [ObservableProperty] IDashStyle? _gridLinesDashStyle = DefaultGridLinesDashStyle;

        [ObservableProperty] IBrush _gridLinesBoldColor = DefaultGridLinesBoldColor;
        [ObservableProperty] double _gridLinesBoldThickness = DefaultGridLinesBoldThickness;
        [ObservableProperty] IDashStyle? _gridLinesBoldDashStyle = DefaultGridLinesBoldDashStyle;

        //[ObservableProperty] ChartYAxis? _connectedChartYAxis = null;
    }
}
