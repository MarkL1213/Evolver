using Avalonia;
using Avalonia.Media;
using EvolverCore.Models;
using EvolverCore.ViewModels;
using EvolverCore.Views.Components;
using System;
using System.Collections.Generic;
using System.Linq;


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
            IndicatorViewModel? vm = Properties.Indicator;
            if (vm == null || vm.Indicator == null) return 0;

            IEnumerable<(IDataPoint, int)> vBars = vm.Indicator.SelectOutputPointsInRange(rangeMin, rangeMax, Properties.PlotIndex);
            return vBars.Count() > 0 ? vBars.Min(p => p.Item1.Y) : 0;
        }
        public double MaxY(DateTime rangeMin, DateTime rangeMax)
        {
            IndicatorViewModel? vm = Properties.Indicator;
            if (vm == null || vm.Indicator == null) return 100;

            IEnumerable<(IDataPoint, int)> vBars = vm.Indicator.SelectOutputPointsInRange(rangeMin, rangeMax, Properties.PlotIndex);

            //return vBars.Count() > 0 ? vBars.Max(p => new BarPricePoint(p as TimeDataBar, Properties.PriceField).Y) : 100;
            return vBars.Count() > 0 ? vBars.Max(p => p.Item1.Y) : 100;
        }

        public void Render(DrawingContext context)
        {
            if (Parent.Properties.IsHidden) return;

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
            Indicator indicator = ivm.Indicator;
            int plotIndex = Properties.PlotIndex;

            List<(IDataPoint, int)> visibleDataPoints = indicator.SelectOutputPointsInRange(
                    vm.XAxis.Min, vm.XAxis.Max, plotIndex, skipLeadingNaN: true).ToList();

            List<Point> visibleScreenPoints = visibleDataPoints
                .Select(p => new Point(
                    ChartPanel.MapXToScreen(vm.XAxis, p.Item1.X, bounds),
                    ChartPanel.MapYToScreen(vm.YAxis, p.Item1.Y, bounds)))
                .ToList();

            if (visibleScreenPoints.Count() < 2) return;

            //////////////
            //FIXME : optimize this to pre-select the visible range only
            List<PlotProperties> propertiesList = indicator.Outputs[plotIndex].Properties.ToList();
            //////////////

            // Start the first batch with the first segment (points 0 to 1)
            var currentBatch = new List<Point> { visibleScreenPoints[0], visibleScreenPoints[1] };
            var currentProp = propertiesList[visibleDataPoints[0].Item2];
            var currentPen = currentProp.CreateLinePen();//FIXME : optimize create pen calls to use cached values where possible

            for (int i = 2; i < visibleScreenPoints.Count; i++)
            {
                // The segment being added is from (i-1) to i, styled by prop at (i-1)
                PlotProperties segProp = propertiesList[visibleDataPoints[i - 1].Item2];

                if (currentProp.ValueEquals(segProp))
                {
                    currentBatch.Add(visibleScreenPoints[i]);
                }
                else
                {
                    if (currentBatch.Count >= 2)
                    {
                        var geometry = new PolylineGeometry(currentBatch, false);
                        context.DrawGeometry(null, currentPen, geometry);
                    }

                    currentBatch.Clear();
                    currentBatch.Add(visibleScreenPoints[i - 1]);
                    currentBatch.Add(visibleScreenPoints[i]);
                    currentProp = segProp;
                    currentPen = segProp.CreateLinePen();//FIXME : optimize create pen calls to use cached values where possible
                }
            }


            if (currentBatch.Count >= 2)
            {
                var geometry = new PolylineGeometry(currentBatch, false);
                context.DrawGeometry(null, currentPen, geometry);
            }

            //var geometry = new PolylineGeometry(visibleScreenPoints, false);
            //context.DrawGeometry(null, _cachedPlotLinePen, geometry);
        }

        private void DrawHistogram(DrawingContext context)
        {
            ChartPlotViewModel plotVM = Properties;
            IndicatorViewModel? ivm = Properties.Indicator;
            if (ivm == null || ivm.Indicator == null || ivm.Indicator.OutputElementCount(Properties.PlotIndex) == 0) return;

            ChartPanelViewModel? panelVM = Parent.Parent.DataContext as ChartPanelViewModel;
            if (panelVM == null || panelVM.XAxis == null) return;
            Rect bounds = Parent.Parent.Bounds;
            Indicator indicator = ivm.Indicator;
            int plotIndex = Properties.PlotIndex;


            TimeSpan xSpan = panelVM.XAxis.Max - panelVM.XAxis.Min;
            double yRange = panelVM.YAxis.Max - panelVM.YAxis.Min;
            if (xSpan <= TimeSpan.Zero || yRange <= 0) return;
            double pixelsPerTick = bounds.Width / xSpan.Ticks;

            double halfBarWidth = Math.Max(2, Math.Min(12, pixelsPerTick * indicator.Interval.Ticks / 2)); // auto-scale width

            List<(IDataPoint, int)> visiblePoints = indicator.SelectOutputPointsInRange(panelVM.XAxis.Min, panelVM.XAxis.Max, plotIndex).ToList();

            //////////////
            //FIXME : optimize this to pre-select the visible range only
            List<PlotProperties> propertiesList = indicator.Outputs[plotIndex].Properties.ToList();
            //////////////

            // Start the first batch with the first bar (point 0)
            var currentBatch = new List<IDataPoint> { visiblePoints[0].Item1 };
            var currentProp = propertiesList[visiblePoints[0].Item2];
            var currentPen = currentProp.CreateLinePen();//FIXME : optimize create pen calls to use cached values where possible
            var currentFill = currentProp.PlotFillBrush;



            for (int i = 1; i < visiblePoints.Count; i++)
            {
                PlotProperties nextBarProp = propertiesList[visiblePoints[i].Item2];

                if (currentProp.ValueEquals(nextBarProp))
                {
                    currentBatch.Add(visiblePoints[i].Item1);
                }
                else
                {
                    if (currentBatch.Count > 0)
                    {
                        foreach (IDataPoint dataPoint in currentBatch)
                        {
                            double xCenter = ChartPanel.MapXToScreen(panelVM.XAxis, dataPoint.X, bounds);
                            double zeroY = ChartPanel.MapYToScreen(panelVM.YAxis, 0, bounds);
                            double volumeY = ChartPanel.MapYToScreen(panelVM.YAxis, dataPoint.Y, bounds);

                            var rect = new Rect(xCenter - halfBarWidth, volumeY, halfBarWidth * 2.0, zeroY - volumeY);

                            if (currentFill != null) { context.FillRectangle(currentFill, rect); }
                            context.DrawRectangle(currentPen, rect);
                        }
                    }


                    currentBatch.Clear();
                    currentBatch.Add(visiblePoints[i].Item1);
                    currentProp = nextBarProp;
                    currentPen = nextBarProp.CreateLinePen();//FIXME : optimize create pen calls to use cached values where possible
                    currentFill = nextBarProp.PlotFillBrush;
                }
            }

            if (currentBatch.Count > 0)
            {
                foreach (IDataPoint dataPoint in currentBatch)
                {
                    double xCenter = ChartPanel.MapXToScreen(panelVM.XAxis, dataPoint.X, bounds);
                    double zeroY = ChartPanel.MapYToScreen(panelVM.YAxis, 0, bounds);
                    double volumeY = ChartPanel.MapYToScreen(panelVM.YAxis, dataPoint.Y, bounds);

                    var rect = new Rect(xCenter - halfBarWidth, volumeY, halfBarWidth * 2.0, zeroY - volumeY);

                    if (currentFill != null) { context.FillRectangle(currentFill, rect); }
                    context.DrawRectangle(currentPen, rect);
                }
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
