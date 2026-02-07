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

            if (vm.Indicator.IsDataOnly)
            {
                BarTablePointer vBars = vm.Indicator.SliceSourcePointsInRange(rangeMin, rangeMax);
                return vBars.RowCount > 0 ? vBars.LowestLow() : 0;
            }
            else
            {
                ColumnPointer<double> vBars = vm.Indicator.SliceOutputPointsInRange(rangeMin, rangeMax, Properties.PlotIndex);
                return vBars.Count > 0 ? vBars.Min() : 0;
            }
        }
        public double MaxY(DateTime rangeMin, DateTime rangeMax)
        {
            IndicatorViewModel? vm = Properties.Indicator;
            if (vm == null || vm.Indicator == null) return 100;

            if (vm.Indicator.IsDataOnly)
            {
                BarTablePointer vBars = vm.Indicator.SliceSourcePointsInRange(rangeMin, rangeMax);
                return vBars.RowCount > 0 ? vBars.HighestHigh() : 0;
            }
            else
            {
                ColumnPointer<double> vBars = vm.Indicator.SliceOutputPointsInRange(rangeMin, rangeMax, Properties.PlotIndex);
                return vBars.Count > 0 ? vBars.Max() : 100;
            }
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

            (int minVisibleIndex, int  maxVisibleIndex) = indicator.IndexOfOutputPointsInRange(vm.XAxis.Min, vm.XAxis.Max, plotIndex);

            List<Point> visibleScreenPoints = new List<Point>();
            List<int> visibleScreenIndexes = new List<int>();
            for (int i = minVisibleIndex; i <= maxVisibleIndex; i++)
            {
                visibleScreenPoints.Add(new Point(
                    ChartPanel.MapXToScreen(vm.XAxis, indicator.Bars[0].Time.GetValueAt(i), bounds),
                    ChartPanel.MapYToScreen(vm.YAxis, indicator.Outputs[plotIndex].GetValueAt(i), bounds)));
                visibleScreenIndexes.Add(i);
            }

            if (visibleScreenPoints.Count() < 2) return;

            List<PlotProperties> propertiesList = indicator.Plots[plotIndex].Properties.GetRange(minVisibleIndex, maxVisibleIndex - minVisibleIndex);

            int n = 0;
            for (int i = 0; i < visibleScreenPoints.Count-1; i++)
            {
                if (!double.IsNaN(indicator.Outputs[plotIndex].GetValueAt(visibleScreenIndexes[i]))) { n = i; break; }
            }

            var currentBatch = new List<Point> { visibleScreenPoints[n] };
            var currentProp = propertiesList[n];
            var currentPen = currentProp.CreateLinePen();//FIXME : optimize create pen calls to use cached values where possible

            for (int i = n+1; i < visibleScreenPoints.Count; i++)
            {
                // The segment being added is from (i-1) to i, styled by prop at (i-1)
                PlotProperties segProp = propertiesList[i - 1];

                if (double.IsNaN(indicator.Outputs[plotIndex].GetValueAt(visibleScreenIndexes[i])))
                {//draw the current batch an skip
                    if (currentBatch.Count >= 2)
                    {
                        var geometry = new PolylineGeometry(currentBatch, false);
                        context.DrawGeometry(null, currentPen, geometry);
                    }
                    else if (currentBatch.Count == 1)
                    {
                        context.FillRectangle(currentPen.Brush!, new Rect(currentBatch[0].X, currentBatch[0].Y, 1, 1));
                    }

                    currentBatch.Clear();
                    continue;
                }

                if (currentBatch.Count == 0)
                {//skip forward until we get 2 valid points...
                    currentBatch.Add(visibleScreenPoints[i]);
                    currentProp = propertiesList[visibleScreenIndexes[i]];
                    currentPen = currentProp.CreateLinePen();
                    continue;
                }

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
            else if (currentBatch.Count == 1)
            {
                context.FillRectangle(currentPen.Brush!, new Rect(currentBatch[0].X, currentBatch[0].Y, 1, 1));
            }
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

            (int minVisibleIndex, int maxVisibleIndex) = indicator.IndexOfOutputPointsInRange(panelVM.XAxis.Min, panelVM.XAxis.Max, plotIndex);
            List<PlotProperties> propertiesList = indicator.Plots[plotIndex].Properties.GetRange(minVisibleIndex,maxVisibleIndex);


            DataTableColumn<double> outputColumn = indicator.Outputs[plotIndex];
            int n = 0;
            for (int i = 0; i < propertiesList.Count - 1; i++)
            {
                if (!double.IsNaN(outputColumn.GetValueAt(i + minVisibleIndex))) { n = i; break; }
            }

            // Start the first batch with the first bar (point 0)
            var currentBatch = new List<int> { n };
            var currentProp = propertiesList[n];
            var currentPen = currentProp.CreateLinePen();//FIXME : optimize create pen calls to use cached values where possible
            var currentFill = currentProp.PlotFillBrush;



            for (int i = n+1; i < propertiesList.Count; i++)
            {
                PlotProperties nextBarProp = propertiesList[i];

                if (double.IsNaN(outputColumn.GetValueAt(i + minVisibleIndex)))
                {
                    if (currentBatch.Count > 0)
                    {
                        foreach (int minOffset in currentBatch)
                        {
                            double xCenter = ChartPanel.MapXToScreen(panelVM.XAxis, indicator.Bars[0].Time.GetValueAt(minOffset + minVisibleIndex), bounds);
                            double zeroY = ChartPanel.MapYToScreen(panelVM.YAxis, 0, bounds);
                            double volumeY = ChartPanel.MapYToScreen(panelVM.YAxis, outputColumn.GetValueAt(minOffset + minVisibleIndex), bounds);

                            var rect = new Rect(xCenter - halfBarWidth, volumeY, halfBarWidth * 2.0, zeroY - volumeY);

                            if (currentFill != null) { context.FillRectangle(currentFill, rect); }
                            context.DrawRectangle(currentPen, rect);
                        }
                    }

                    currentBatch.Clear();
                    continue;
                }


                if (currentProp.ValueEquals(nextBarProp))
                {
                    currentBatch.Add(i);
                }
                else
                {
                    if (currentBatch.Count > 0)
                    {
                        foreach (int minOffset in currentBatch)
                        {
                            double xCenter = ChartPanel.MapXToScreen(panelVM.XAxis, indicator.Bars[0].Time.GetValueAt(minOffset + minVisibleIndex), bounds);
                            double zeroY = ChartPanel.MapYToScreen(panelVM.YAxis, 0, bounds);
                            double volumeY = ChartPanel.MapYToScreen(panelVM.YAxis, outputColumn.GetValueAt(minOffset + minVisibleIndex), bounds);

                            var rect = new Rect(xCenter - halfBarWidth, volumeY, halfBarWidth * 2.0, zeroY - volumeY);

                            if (currentFill != null) { context.FillRectangle(currentFill, rect); }
                            context.DrawRectangle(currentPen, rect);
                        }
                    }


                    currentBatch.Clear();
                    currentBatch.Add(i);
                    currentProp = nextBarProp;
                    currentPen = nextBarProp.CreateLinePen();//FIXME : optimize create pen calls to use cached values where possible
                    currentFill = nextBarProp.PlotFillBrush;
                }
            }

            if (currentBatch.Count > 0)
            {
                foreach (int minOffset in currentBatch)
                {
                    double xCenter = ChartPanel.MapXToScreen(panelVM.XAxis, indicator.Bars[0].Time.GetValueAt(minOffset + minVisibleIndex), bounds);
                    double zeroY = ChartPanel.MapYToScreen(panelVM.YAxis, 0, bounds);
                    double volumeY = ChartPanel.MapYToScreen(panelVM.YAxis, outputColumn.GetValueAt(minOffset + minVisibleIndex), bounds);

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

            double halfBarWidth = Math.Max(2, Math.Min(12, pixelsPerTick * indicator.Interval.Ticks / 2)); // auto-scale width

            (int minVisibleIndex,int maxVisibleIndex) = indicator.IndexOfSourcePointsInRange(
                                            indicator.Interval.RoundUp(xAxis.Min),
                                            indicator.Interval.RoundDown(xAxis.Max));
            if (minVisibleIndex == -1 || maxVisibleIndex == -1) return;

            BarTablePointer bars = indicator.Bars[0];

            for (int i=minVisibleIndex;i<=maxVisibleIndex;i++)
            {
                DateTime time = bars.Time.GetValueAt(i);
                if (time < xAxis.Min || time > xAxis.Max) continue;

                double xCenter = (time - xAxis.Min).Ticks * pixelsPerTick;

                double highY = bounds.Height - (bars.High.GetValueAt(i) - yAxis.Min) / yRange * bounds.Height;
                double lowY = bounds.Height - (bars.Low.GetValueAt(i) - yAxis.Min) / yRange * bounds.Height;

                double open = bars.Open.GetValueAt(i);
                double close = bars.Close.GetValueAt(i);

                double openY = bounds.Height - (open - yAxis.Min) / yRange * bounds.Height;
                double closeY = bounds.Height - (close - yAxis.Min) / yRange * bounds.Height;

                bool isBull = close >= open;
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
