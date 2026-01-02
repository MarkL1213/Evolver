using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using EvolverCore.ViewModels;
using EvolverCore.Views.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvolverCore.Models;


namespace EvolverCore.Views
{
    public enum PlotStyle
    {
        Line,
        Bar,
        Candlestick
    }

    internal class ChartPlot : AvaloniaObject
    {
        internal ChartPlot(ChartComponentBase parent)
        {
            Parent = parent;

            AvaloniaProperty[] penProperties =
            {
                PlotLineColorProperty,
                PlotLineStyleProperty,
                PlotLineThicknessProperty
            };

            foreach (AvaloniaProperty p in penProperties)
                p.Changed.AddClassHandler<ChartPlot>((c, _) => c.InvalidatePenCache());
        }

        ChartPlotViewModel _properties = new ChartPlotViewModel();
        internal ChartPlotViewModel Properties
        {
            get { return _properties; }
            set
            {
                if (_properties != null)
                {
                    _properties.PropertyChanged -= (_, __) => OnPropertyChanged();
                }

                _properties = value;

                if (_properties != null)
                {
                    _properties.PropertyChanged += (_, __) => OnPropertyChanged();
                    OnPropertiesChanged();
                }
            }
        }

        internal ChartComponentBase Parent { get; private set; }

        private void OnPropertiesChanged()
        {
            Style = _properties.Style;
            PlotFillColor = _properties.PlotFillBrush.Color;

            PlotLineColor = _properties.PlotLineBrush.Color;
            PlotLineThickness = _properties.PlotLineThickness;
            PlotLineStyle = _properties.PlotLineStyle.Style;

            InvalidatePenCache();
        }

        private void OnPropertyChanged()
        {
            OnPropertiesChanged();
        }

        #region Style property
        public static readonly StyledProperty<PlotStyle> StyleProperty =
            AvaloniaProperty.Register<ChartPlot, PlotStyle>(nameof(Style), PlotStyle.Line);
        public PlotStyle Style
        {
            get { return GetValue(StyleProperty); }
            set { SetValue(StyleProperty, value); }
        }
        #endregion

        #region PlotFillColor property
        public static readonly StyledProperty<IBrush?> PlotFillColorProperty =
            AvaloniaProperty.Register<ChartPlot, IBrush?>(nameof(PlotFillColor), Brushes.Cyan);
        public IBrush? PlotFillColor
        {
            get { return GetValue(PlotFillColorProperty); }
            set { SetValue(PlotFillColorProperty, value); }
        }
        #endregion

        #region PlotLine pen properties
        private Pen? _cachedPlotLinePen;
        public static readonly StyledProperty<IBrush?> PlotLineColorProperty =
            AvaloniaProperty.Register<ChartPlot, IBrush?>(nameof(PlotLineColor), Brushes.Cyan);
        public IBrush? PlotLineColor
        {
            get { return GetValue(PlotLineColorProperty); }
            set { SetValue(PlotLineColorProperty, value); }
        }

        public static readonly StyledProperty<double> PlotLineThicknessProperty =
            AvaloniaProperty.Register<ChartPlot, double>(nameof(PlotLineThickness), 1.5);
        public double PlotLineThickness
        {
            get { return GetValue(PlotLineThicknessProperty); }
            set { SetValue(PlotLineThicknessProperty, value); }
        }

        public static readonly StyledProperty<IDashStyle?> PlotLineStyleProperty =
            AvaloniaProperty.Register<ChartPlot, IDashStyle?>(nameof(PlotLineStyle), null);
        public IDashStyle? PlotLineStyle
        {
            get { return GetValue(PlotLineStyleProperty); }
            set { SetValue(PlotLineStyleProperty, value); }
        }
        #endregion

        Pen? _cachedWickPen = null;
        Pen? _cachedCandleOutlinePen = null;

        internal void InvalidatePenCache()
        {
            _cachedPlotLinePen = null;
            _cachedWickPen = null;
            _cachedCandleOutlinePen = null;
        }

        public double MinY(DateTime rangeMin, DateTime rangeMax)
        {
            return 0;
        }
        public double MaxY(DateTime rangeMin, DateTime rangeMax)
        {
            IndicatorViewModel? vm = Properties.Indicator;
            if (vm == null || vm.Indicator == null) return 100;

            IEnumerable<IDataPoint> vBars = vm.Indicator.SelectOutputPointsInRange(rangeMin, rangeMax, Properties.PlotIndex);

            return vBars.Count() > 0 ? vBars.Max(p => new BarPricePoint(p as TimeDataBar, Properties.PriceField).Y) : 100;
        }

        public void Render(DrawingContext context)
        {
            if(Parent.Properties.IsHidden) return;

            if (Style == PlotStyle.Bar) { DrawHistogram(context); }
            else if (Style == PlotStyle.Line) { DrawCurve(context); }
            else if (Style == PlotStyle.Candlestick) { DrawCandlesticks(context); }
        }

        private void DrawCurve(DrawingContext context)
        {
            ChartPanelViewModel? vm = Parent.Parent.DataContext as ChartPanelViewModel;
            IndicatorViewModel? ivm = Properties.Indicator;


            if (vm == null || vm.XAxis == null || ivm == null || ivm.Indicator == null || ivm.Indicator.OutputElementCount(Properties.PlotIndex) == 0) { return; }
            Rect bounds = Parent.Parent.Bounds;

            _cachedPlotLinePen ??= new Pen(PlotLineColor, PlotLineThickness, PlotLineStyle);

            IEnumerable<IDataPoint> visibleDataPoints = ivm.Indicator.SelectOutputPointsInRange(vm.XAxis.Min, vm.XAxis.Max, Properties.PlotIndex, true);
            IEnumerable<Point> visibleScreenPoints = visibleDataPoints.Select<IDataPoint, Point>(p => new Point(ChartPanel.MapXToScreen(vm.XAxis, p.X, bounds), ChartPanel.MapYToScreen(vm.YAxis, p.Y, bounds)));

            if (visibleScreenPoints.Count() < 2) return;

            var geometry = new PolylineGeometry(visibleScreenPoints, false);
            context.DrawGeometry(null, _cachedPlotLinePen, geometry);
        }

        private void DrawHistogram(DrawingContext context)
        {
            ChartPlotViewModel plotVM = Properties;
            IndicatorViewModel? ivm = Properties.Indicator;
            if (ivm == null || ivm.Indicator == null || ivm.Indicator.OutputElementCount(Properties.PlotIndex) == 0) return;

            ChartPanelViewModel? panelVM = Parent.Parent.DataContext as ChartPanelViewModel;
            if (panelVM == null || panelVM.XAxis == null) return;
            Rect bounds = Parent.Parent.Bounds;

            TimeSpan xSpan = panelVM.XAxis.Max - panelVM.XAxis.Min;
            double yRange = panelVM.YAxis.Max - panelVM.YAxis.Min;
            if (xSpan <= TimeSpan.Zero || yRange <= 0) return;
            double pixelsPerTick = bounds.Width / xSpan.Ticks;

            double halfBarWidth = Math.Max(2, Math.Min(12, pixelsPerTick * ivm.Indicator.Interval.Ticks / 2)); // auto-scale width

            IEnumerable<IDataPoint> visiblePoints = ivm.Indicator.SelectOutputPointsInRange(panelVM.XAxis.Min, panelVM.XAxis.Max,plotVM.PlotIndex);

            foreach (var dataPoint in visiblePoints)
            {
                if (dataPoint == null) continue;

                double xCenter = ChartPanel.MapXToScreen(panelVM.XAxis, dataPoint.X, bounds);
                double zeroY = ChartPanel.MapYToScreen(panelVM.YAxis, 0, bounds);
                double volumeY = ChartPanel.MapYToScreen(panelVM.YAxis, dataPoint.Y, bounds);

                var fillBrush = Properties.PlotFillBrush.Color;

                _cachedPlotLinePen ??= new Pen(Properties.PlotLineBrush.Color, Properties.PlotLineThickness, Properties.PlotLineStyle.Style);
                if (fillBrush == null || _cachedPlotLinePen == null) { return; }

                var rect = new Rect(xCenter - halfBarWidth, volumeY, halfBarWidth * 2.0, zeroY - volumeY);

                context.FillRectangle(fillBrush, rect);
                context.DrawRectangle(_cachedPlotLinePen, rect);
            }
        }

        private void DrawCandlesticks(DrawingContext context)
        {
            DataPlotViewModel? plotVM = Properties as DataPlotViewModel;
            if (plotVM == null || Properties.Indicator == null || Properties.Indicator.Indicator == null) return;
            Indicator indicator = Properties.Indicator.Indicator;

            ChartPanelViewModel? panelVM = Parent.Parent.DataContext as ChartPanelViewModel;
            if (panelVM == null) return;
            if (panelVM.XAxis is not { } xAxis || panelVM.YAxis is not { } yAxis) return;

            Rect bounds = Parent.Parent.Bounds;

            TimeSpan xSpan = xAxis.Max - xAxis.Min;
            double yRange = yAxis.Max - yAxis.Min;
            if (xSpan <= TimeSpan.Zero || yRange <= 0) return;
            double pixelsPerTick = bounds.Width / xSpan.Ticks;

            ChartPanel panel = Parent.Parent;

            double halfBarWidth = Math.Max(2, Math.Min(12, pixelsPerTick * Properties.Indicator.Indicator.Interval.Ticks / 2)); // auto-scale width

            IEnumerable<IDataPoint> visiblePoints = Properties.Indicator.Indicator.SelectInputPointsInRange(xAxis.Min, xAxis.Max);

            foreach (var dataPoint in visiblePoints)
            {
                TimeDataBar? bar = dataPoint as TimeDataBar;
                if (bar == null) continue;

                if (bar.Time < xAxis.Min || bar.Time > xAxis.Max) continue;

                double xCenter = (bar.Time - xAxis.Min).Ticks * pixelsPerTick;

                double highY = bounds.Height - (bar.High - yAxis.Min) / yRange * bounds.Height;
                double lowY = bounds.Height - (bar.Low - yAxis.Min) / yRange * bounds.Height;
                double openY = bounds.Height - (bar.Open - yAxis.Min) / yRange * bounds.Height;
                double closeY = bounds.Height - (bar.Close - yAxis.Min) / yRange * bounds.Height;

                bool isBull = bar.Close >= bar.Open;
                var bodyBrush = isBull ? plotVM.CandleUpColor : plotVM.CandleDownColor;
                _cachedWickPen ??= new Pen(plotVM.WickColor, plotVM.WickThickness, plotVM.WickDashStyle);
                _cachedCandleOutlinePen ??= new Pen(plotVM.CandleOutlineColor, plotVM.CandleOutlineThickness, plotVM.CandleOutlineDashStyle);

                // Wick (high-low line)
                context.DrawLine(_cachedWickPen, new Point(xCenter, highY), new Point(xCenter, lowY));

                // Body (rectangle)
                double bodyTop = Math.Min(openY, closeY);
                double bodyBottom = Math.Max(openY, closeY);
                double bodyHeight = bodyBottom - bodyTop;

                if (bodyHeight < 1) // Doji or very small body to draw as line
                {
                    context.DrawLine(_cachedWickPen, new Point(xCenter - halfBarWidth, bodyTop),
                                            new Point(xCenter + halfBarWidth, bodyTop));
                }
                else
                {
                    var bodyRect = new Rect(xCenter - halfBarWidth, bodyTop, halfBarWidth * 2, bodyHeight);
                    context.DrawRectangle(bodyBrush, _cachedCandleOutlinePen, bodyRect);
                    if (bodyHeight > 1)
                    {
                        // Top/bottom lines for open/close levels
                        context.DrawLine(_cachedCandleOutlinePen, new Point(xCenter - halfBarWidth, openY), new Point(xCenter + halfBarWidth, openY));
                        if (openY != closeY)  // Avoid double line on doji
                            context.DrawLine(_cachedCandleOutlinePen, new Point(xCenter - halfBarWidth, closeY), new Point(xCenter + halfBarWidth, closeY));
                    }
                }
            }
        }
    }
}
