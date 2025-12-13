using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using EvolverCore.ViewModels;
using EvolverCore.Views.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Views
{
    public enum PlotStyle
    {
        Line,
        Bar
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

        internal ChartPlotViewModel Properties { get; set; } = new ChartPlotViewModel();

        internal ChartComponentBase Parent { get; private set; }

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
        public static readonly StyledProperty<IBrush> PlotFillColorProperty =
            AvaloniaProperty.Register<ChartPlot, IBrush>(nameof(PlotFillColor), Brushes.Cyan);
        public IBrush PlotFillColor
        {
            get { return GetValue(PlotFillColorProperty); }
            set { SetValue(PlotFillColorProperty, value); }
        }
        #endregion

        #region PlotLine pen properties
        private Pen? _cachedPlotLinePen;
        public static readonly StyledProperty<IBrush> PlotLineColorProperty =
            AvaloniaProperty.Register<ChartPlot, IBrush>(nameof(PlotLineColor), Brushes.Cyan);
        public IBrush PlotLineColor
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

        internal void InvalidatePenCache() { _cachedPlotLinePen = null; }

        public void Render(DrawingContext context)
        {
            if (Style == PlotStyle.Bar) { DrawHistogram(context); }
            else if (Style == PlotStyle.Line) { DrawCurve(context); }
        }

        private void DrawCurve(DrawingContext context)
        {
            ChartPanelViewModel? vm = Parent.Parent.DataContext as ChartPanelViewModel;
            ChartComponentViewModel cm = Parent.Properties;
            if (vm == null || vm.XAxis == null || cm.Data == null) { return; }
            Rect bounds = Parent.Parent.Bounds;

            _cachedPlotLinePen ??= new Pen(PlotLineColor, PlotLineThickness, PlotLineStyle);

            List<Point> visiblePoints;
            if (cm.Data[0] is TimeDataBar)
            {
                List<BarPricePoint> visibleDataPoints = cm.Data
                        .Where(p => p.X >= vm.XAxis.Min && p.X <= vm.XAxis.Max)
                        .Select(p => new BarPricePoint(p as TimeDataBar,Properties.PriceField))
                        .ToList();
                visiblePoints = visibleDataPoints
                        .Select(p => new Point(ChartPanel.MapXToScreen(vm.XAxis, p.X, bounds), ChartPanel.MapYToScreen(vm.YAxis, p.Y, bounds)))
                        .ToList();
            }
            else
            {
                visiblePoints = cm.Data
                    .Where(p => p.X >= vm.XAxis.Min && p.X <= vm.XAxis.Max)
                    .Select<IDataPoint, Point>(p => new Point(ChartPanel.MapXToScreen(vm.XAxis, p.X, bounds), ChartPanel.MapYToScreen(vm.YAxis, p.Y, bounds))).
                    ToList();
            }

            if (visiblePoints.Count < 2) return;

            var geometry = new PolylineGeometry(visiblePoints, false);
            context.DrawGeometry(null, _cachedPlotLinePen, geometry);
        }

        private void DrawHistogram(DrawingContext context)
        {
            ChartPanelViewModel? vm = Parent.Parent.DataContext as ChartPanelViewModel;
            ChartComponentViewModel cm = Parent.Properties;
            if (vm == null || vm.XAxis == null || cm.Data == null) return;
            Rect bounds = Parent.Parent.Bounds;

            _cachedPlotLinePen ??= new Pen(PlotLineColor, PlotLineThickness, PlotLineStyle);

            TimeSpan xSpan = vm.XAxis.Max - vm.XAxis.Min;
            if (xSpan <= TimeSpan.Zero) return;

            List<IDataPoint> visiblePoints = cm.Data
                .Where(p => p.X >= vm.XAxis.Min && p.X <= vm.XAxis.Max)
                .ToList();

            // Find max value in visible range for proper scaling
            double maxValue = 0;
            foreach (IDataPoint dataPoint in visiblePoints) { maxValue = Math.Max(maxValue, dataPoint.Y); }

            if (maxValue == 0) return;

            double pixelsPerTick = bounds.Width / xSpan.TotalMilliseconds;
            double barWidth = pixelsPerTick * DataSeries<TimeDataPoint>.IntervalTicks(cm.Data);
            double maxBarHeight = bounds.Height * 0.9; // Tallest bar takes ~90% of panel height (adjustable)

            foreach (IDataPoint dataPoint in visiblePoints)
            {
                if (dataPoint.X < vm.XAxis.Min || dataPoint.X > vm.XAxis.Max) continue;

                double xCenter = (dataPoint.X - vm.XAxis.Min).TotalMilliseconds * pixelsPerTick;
                TimeDataBar? tdb = dataPoint as TimeDataBar;
                double Y = tdb != null ? new BarPricePoint(tdb, Properties.PriceField).Y : dataPoint.Y;
                double valueRatio = Y / maxValue;
                double barHeight = valueRatio * maxBarHeight;

                var rect = new Rect(
                    xCenter - barWidth * 0.4,
                    bounds.Height - barHeight,
                    barWidth * 0.8,
                    barHeight);

                context.DrawRectangle(PlotFillColor, _cachedPlotLinePen, rect);
            }
        }
    }
}
