using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;

using EvolverCore.Data;
using EvolverCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EvolverCore;

public partial class ChartPanel : Decorator
{
    static ChartPanel()
    {
        AffectsRender<ChartPanel>(
            BackgroundColorProperty,
            ShowGridLinesProperty,
            GridLinesColorProperty,
            GridLinesThicknessProperty,
            GridLinesDashStyleProperty,
            GridLinesBoldColorProperty,
            GridLinesBoldThicknessProperty,
            GridLinesBoldDashStyleProperty
            );

        AvaloniaProperty[] penProperties =
        {
            GridLinesColorProperty,
            GridLinesThicknessProperty,
            GridLinesDashStyleProperty,
            GridLinesBoldColorProperty,
            GridLinesBoldThicknessProperty,
            GridLinesBoldDashStyleProperty
        };
        
        foreach (AvaloniaProperty p in penProperties)
            p.Changed.AddClassHandler<ChartPanel>((c, _) => c.InvalidatePenCache());
    }

    public ChartPanel()
    {
        InitializeComponent();

        PointerWheelChanged += OnPointerWheelChanged;
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;

        _candleBullBrush = new SolidColorBrush(Colors.LimeGreen, 0.7);
        _candleBearBrush = new SolidColorBrush(Colors.Red, 0.7);
        _candleBullPen = new Pen(Brushes.LimeGreen, 1.5);
        _candleBearPen = new Pen(Brushes.Red, 1.5);
    }


    private Point _dragStart;
    private DateTime _dragStartXMin;
    private DateTime _dragStartXMax;
    private double _dragStartYMin;
    private double _dragStartYMax;
    private bool _isDragging;
    private ChartPanelViewModel? _vm;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm != null)
        {
            if (_vm.XAxis != null) _vm.XAxis.PropertyChanged -= AxisPropertyChanged;
            _vm.YAxis.PropertyChanged -= AxisPropertyChanged;
        }
        
        _vm = DataContext as ChartPanelViewModel;

        if (_vm != null)
        {
            if (_vm.XAxis != null) _vm.XAxis.PropertyChanged += AxisPropertyChanged;
            _vm.YAxis.PropertyChanged += AxisPropertyChanged;
        }
    }

    private void AxisPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_vm == null || _vm.XAxis == null)
        {
            e.Handled = true;
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStart = e.GetPosition(this);
            _dragStartXMin = _vm.XAxis.Min;
            _dragStartXMax = _vm.XAxis.Max;
            _dragStartYMin = _vm.YAxis.Min;
            _dragStartYMax = _vm.YAxis.Max;
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || _vm == null || _vm.XAxis == null)
        {
            e.Handled = true;
            return;
        }


        var currentPos = e.GetPosition(this);
        var delta = currentPos - _dragStart; // delta.X and delta.Y in pixels

        if (Math.Abs(delta.X) > 0.5) // avoid jitter
        {
            TimeSpan currentSpan = _dragStartXMax - _dragStartXMin;
            if (currentSpan != TimeSpan.Zero)
            {
                // Convert pixel drag distance to world time offset
                double pixelsPerTick = Bounds.Width / (double)currentSpan.Ticks;
                long ticksToShift = (long)(-delta.X * pixelsPerTick); // negative = drag right to pan left

                _vm.XAxis.Min = _dragStartXMin.AddTicks(ticksToShift);
                _vm.XAxis.Max = _dragStartXMax.AddTicks(ticksToShift);
            }
        }

        if (Math.Abs(delta.Y) > 0.5)
        {
            double currentRange = _dragStartYMax - _dragStartYMin;
            if (currentRange > 0)
            {
                double pixelsPerUnit = Bounds.Height / currentRange;
                double unitsToShift = delta.Y * pixelsPerUnit; // positive delta.Y = drag down to view moves down

                _vm.YAxis.Min = _dragStartYMin + unitsToShift;
                _vm.YAxis.Max = _dragStartYMax + unitsToShift;
            }
        }

        InvalidateVisual();  // Immediate redraw
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging)
        {
            e.Handled = true;
            return;
        }

        _isDragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;

    }

    private void OnPointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        if (_vm == null || _vm.XAxis == null) return;

        double zoomFactor = e.Delta.Y > 0 ? 0.9 : 1.1; // Zoom in (<1) or out (>1)

        var mousePos = e.GetPosition(this);
        double mouseNormX = mousePos.X / Bounds.Width;
        double mouseNormY = 1.0 - (mousePos.Y / Bounds.Height); // Invert Y (top = 0)

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            TimeSpan totalSpan = _vm.XAxis.Max - _vm.XAxis.Min;
            if (totalSpan == TimeSpan.Zero) return;

            // Find the DateTime value at the mouse X position
            DateTime mouseDate = _vm.XAxis.Min + TimeSpan.FromTicks((long)(mouseNormX * totalSpan.Ticks));

            // Distance from mouse to left/right edges (as TimeSpan)
            TimeSpan leftSpan = mouseDate - _vm.XAxis.Min;
            TimeSpan rightSpan = _vm.XAxis.Max - mouseDate;

            // Apply zoom factor symmetrically around mouse
            _vm.XAxis.Min = mouseDate - TimeSpan.FromTicks((long)(leftSpan.Ticks * zoomFactor));
            _vm.XAxis.Max = mouseDate + TimeSpan.FromTicks((long)(rightSpan.Ticks * zoomFactor));
        }
        else
        {
            double totalRange = _vm.YAxis.Max - _vm.YAxis.Min;
            if (totalRange <= 0) return;

            double mouseValue = _vm.YAxis.Min + mouseNormY * totalRange;

            double lowerDist = mouseValue - _vm.YAxis.Min;
            double upperDist = _vm.YAxis.Max - mouseValue;

            _vm.YAxis.Min = mouseValue - lowerDist * zoomFactor;
            _vm.YAxis.Max = mouseValue + upperDist * zoomFactor;
        }

        InvalidateVisual();
        e.Handled = true;
    }

    // Mapping helpers (add to class)
    internal static double MapXToScreen(ChartXAxisViewModel vm,DateTime dateX, Rect bounds)
    {
        if (vm == null) return 0;
        var span = vm.Max - vm.Min;
        if (span <= TimeSpan.Zero) return 0;

        return (dateX - vm.Min).TotalMilliseconds / span.TotalMilliseconds * bounds.Width;
    }

    internal static double MapYToScreen(ChartYAxisViewModel vm,double worldY, Rect bounds)
    {
        if (vm == null) return bounds.Height;
        double range = vm.Max - vm.Min;
        if (range <= 0) return bounds.Height;
        return bounds.Height - ((worldY - vm.Min) / range * bounds.Height);  // y=0 at bottom
    }


    public static bool IsMajorTick(DateTime dt) => dt.TimeOfDay == TimeSpan.Zero || dt.Hour % 6 == 0;

    public static List<DateTime> ComputeDateTimeTicks(DateTime min, DateTime max)
    {
        var ticks = new List<DateTime>();
        var range = max - min;
        if(range <= TimeSpan.Zero) return ticks;

        TimeSpan interval = range switch
        {
            var r when r <= TimeSpan.FromMinutes(1) => TimeSpan.FromSeconds(10),
            var r when r <= TimeSpan.FromMinutes(10) => TimeSpan.FromMinutes(1),
            var r when r <= TimeSpan.FromHours(1) => TimeSpan.FromMinutes(10),
            var r when r <= TimeSpan.FromDays(1) => TimeSpan.FromHours(1),
            var r when r <= TimeSpan.FromDays(7) => TimeSpan.FromDays(1),
            _ => TimeSpan.FromDays(1)
        };

        var start = min.Date.AddTicks((min.Ticks / interval.Ticks) * interval.Ticks + interval.Ticks);
        if (start < min) start += interval;

        for (var t = start; t <= max; t += interval)
            ticks.Add(t);

        return ticks;
    }

    public static List<double> ComputeDoubleTicks(double min, double max)
    {
        var ticks = new List<double>();
        double range = max - min;
        if (range <= 0) return ticks;

        double roughInterval = range / 8;
        double order = Math.Pow(10, Math.Floor(Math.Log10(roughInterval)));
        double interval = Math.Ceiling(roughInterval / order) * order;

        double start = Math.Ceiling(min / interval) * interval;
        for (double v = start; v <= max + interval / 2; v += interval)
            ticks.Add(v);

        return ticks;
    }

    private Pen _candleBullPen;
    private Pen _candleBearPen;
    private IBrush _candleBullBrush;
    private IBrush _candleBearBrush;

    #region ShowGridLines property
    public static readonly StyledProperty<bool> ShowGridLinesProperty =
        AvaloniaProperty.Register<ChartPanel, bool>(nameof(ShowGridLines), true);
    public bool ShowGridLines
    {
        get { return GetValue(ShowGridLinesProperty); }
        set { SetValue(ShowGridLinesProperty, value); }
    }
    #endregion

    #region BackgroundColor property
    public static readonly StyledProperty<IBrush> BackgroundColorProperty =
        AvaloniaProperty.Register<ChartPanel, IBrush>(nameof(BackgroundColor), Brushes.Black);
    public IBrush BackgroundColor
    {
        get { return GetValue(BackgroundColorProperty); }
        set { SetValue(BackgroundColorProperty, value); }
    }
    #endregion

    #region GridLines pen properties
    private Pen? _cachedGridLinesPen;

    public static readonly StyledProperty<IBrush> GridLinesColorProperty =
    AvaloniaProperty.Register<ChartPanel, IBrush>(nameof(GridLinesColor), Brushes.DarkGray);
    public IBrush GridLinesColor
    {
        get { return GetValue(GridLinesColorProperty); }
        set { SetValue(GridLinesColorProperty, value); }
    }

    public static readonly StyledProperty<double> GridLinesThicknessProperty =
    AvaloniaProperty.Register<ChartPanel, double>(nameof(GridLinesThickness), 1);
    public double GridLinesThickness
    {
        get { return GetValue(GridLinesThicknessProperty); }
        set { SetValue(GridLinesThicknessProperty, value); }
    }

    public static readonly StyledProperty<IDashStyle?> GridLinesDashStyleProperty =
    AvaloniaProperty.Register<ChartPanel, IDashStyle?>(nameof(GridLinesDashStyle), new ImmutableDashStyle(new double[] { 1, 1 }, 0));
    public IDashStyle? GridLinesDashStyle
    {
        get { return GetValue(GridLinesDashStyleProperty); }
        set { SetValue(GridLinesDashStyleProperty, value); }
    }
    #endregion

    #region GridLinesBold pen properties
    private Pen? _cachedGridLinesBoldPen;

    public static readonly StyledProperty<IBrush> GridLinesBoldColorProperty =
    AvaloniaProperty.Register<ChartPanel, IBrush>(nameof(GridLinesBoldColor), Brushes.Gray);
    public IBrush GridLinesBoldColor
    {
        get { return GetValue(GridLinesBoldColorProperty); }
        set { SetValue(GridLinesBoldColorProperty, value); }
    }

    public static readonly StyledProperty<double> GridLinesBoldThicknessProperty =
    AvaloniaProperty.Register<ChartPanel, double>(nameof(GridLinesBoldThickness), 2);
    public double GridLinesBoldThickness
    {
        get { return GetValue(GridLinesBoldThicknessProperty); }
        set { SetValue(GridLinesBoldThicknessProperty, value); }
    }

    public static readonly StyledProperty<IDashStyle?> GridLinesBoldDashStyleProperty =
    AvaloniaProperty.Register<ChartPanel, IDashStyle?>(nameof(GridLinesBoldDashStyle), null);
    public IDashStyle? GridLinesBoldDashStyle
    {
        get { return GetValue(GridLinesBoldDashStyleProperty); }
        set { SetValue(GridLinesBoldDashStyleProperty, value); }
    }
    #endregion

    #region ConnectedChartYAxis property
    private ChartYAxis? _connectedChartYAxis;
    public ChartYAxis? ConnectedChartYAxis
    {
        get { return _connectedChartYAxis; }
    }
    
    public void SetConnectedChartYAxis(ChartYAxis? chartChartYAxis)
    {
        _connectedChartYAxis = chartChartYAxis;
    }
    #endregion

    private void InvalidatePenCache()
    {
        _cachedGridLinesPen = null;
        _cachedGridLinesBoldPen = null;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        DrawBackground(context);
        if(ShowGridLines) DrawGridLines(context);
        DrawCandlesticks(context);
    }

    private void DrawBackground(DrawingContext context)
    {
        context.FillRectangle(BackgroundColor, new Rect(0, 0, Bounds.Width, Bounds.Height));
    }

    private void DrawGridLines(DrawingContext context)
    {
        if (_vm == null || _vm.XAxis == null || _cachedGridLinesPen == null) return;

        _cachedGridLinesPen ??= new Pen(GridLinesColor, GridLinesThickness, GridLinesDashStyle);
        _cachedGridLinesBoldPen ??= new Pen(GridLinesBoldColor, GridLinesBoldThickness, GridLinesBoldDashStyle);

        var xTicks = ComputeDateTimeTicks(_vm.XAxis.Min,_vm.XAxis.Max);
        foreach (var tick in xTicks)
        {
            double x = MapXToScreen(_vm.XAxis,tick,Bounds);
            var pen = IsMajorTick(tick) ? _cachedGridLinesBoldPen : _cachedGridLinesPen;
            context.DrawLine(pen, new Point(x, 0), new Point(x, Bounds.Height));
        }

        var yTicks = ComputeDoubleTicks(_vm.YAxis.Min, _vm.YAxis.Max);
        foreach (var y in yTicks)
        {
            double screenY = MapYToScreen(_vm.YAxis, y, Bounds);
            context.DrawLine(_cachedGridLinesPen, new Point(0, screenY), new Point(Bounds.Width, screenY));
        }
    }

    private void DrawCandlesticks(DrawingContext context)
    {
        if (_vm?.Data is not BarDataSeries bars || bars.Count == 0) return;
        if (_vm.XAxis is not { } xAxis || _vm.YAxis is not { } yAxis) return;

        TimeSpan xSpan = xAxis.Max - xAxis.Min;
        double yRange = yAxis.Max - yAxis.Min;
        if (xSpan <= TimeSpan.Zero || yRange <= 0) return;

        double pixelsPerTick = Bounds.Width / xSpan.Ticks;
        double halfBarWidth = Math.Max(2, Math.Min(12, pixelsPerTick * BarDataSeries.IntervalTicks(bars) / 2)); // auto-scale width

        foreach (var bar in bars)
        {
            if (bar.Time < xAxis.Min || bar.Time > xAxis.Max) continue;

            double xCenter = (bar.Time - xAxis.Min).Ticks * pixelsPerTick;

            double highY = Bounds.Height - (bar.High - yAxis.Min) / yRange * Bounds.Height;
            double lowY = Bounds.Height - (bar.Low - yAxis.Min) / yRange * Bounds.Height;
            double openY = Bounds.Height - (bar.Open - yAxis.Min) / yRange * Bounds.Height;
            double closeY = Bounds.Height - (bar.Close - yAxis.Min) / yRange * Bounds.Height;

            bool isBull = bar.Close >= bar.Open;
            var bodyBrush = isBull ? _candleBullBrush : _candleBearBrush;
            var wickPen = isBull ? _candleBullPen : _candleBearPen;

            // Wick (high-low line)
            context.DrawLine(wickPen, new Point(xCenter, highY), new Point(xCenter, lowY));

            // Body (rectangle)
            double bodyTop = Math.Min(openY, closeY);
            double bodyBottom = Math.Max(openY, closeY);
            double bodyHeight = bodyBottom - bodyTop;

            if (bodyHeight < 1) // Doji or very small body to draw as line
            {
                context.DrawLine(wickPen, new Point(xCenter - halfBarWidth, bodyTop),
                                        new Point(xCenter + halfBarWidth, bodyTop));
            }
            else
            {
                var bodyRect = new Rect(xCenter - halfBarWidth, bodyTop, halfBarWidth * 2, bodyHeight);
                context.DrawRectangle(bodyBrush, wickPen, bodyRect);
            }
        }
    }

    private void DrawCurve(DrawingContext context)
    {
        if (_vm == null || _vm.XAxis == null || _vm.Data == null || _vm.Data.Count <=0) return;

        // Filter visible points only (huge perf win with large datasets)
        List<Point> visiblePoints = _vm.Data
            .Where(p => p.X >= _vm.XAxis.Min && p.X <= _vm.XAxis.Max)
            .Select<IDataPoint, Point>(p => new Point(MapXToScreen(_vm.XAxis,p.X,Bounds), MapYToScreen(_vm.YAxis,p.Y,Bounds))).
            ToList();
            

        if (visiblePoints.Count < 2) return;

        var geometry = new PolylineGeometry(visiblePoints, false);
        context.DrawGeometry(null, new Pen(Brushes.Cyan, 1.5), geometry);
    }
}