using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using EvolverCore.ViewModels;
using EvolverCore.Views.Components;
using EvolverCore.Views.ContextMenus;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;


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
            GridLinesBoldDashStyleProperty,
            CandleDownColorProperty,
            CandleUpColorProperty,
            WickColorProperty,
            WickDashStyleProperty,
            WickThicknessProperty,
            CandleOutlineThicknessProperty,
            CandleOutlineDashStyleProperty,
            CandleOutlineColorProperty
            );

        AvaloniaProperty[] penProperties =
        {
            GridLinesColorProperty,
            GridLinesThicknessProperty,
            GridLinesDashStyleProperty,
            GridLinesBoldColorProperty,
            GridLinesBoldThicknessProperty,
            GridLinesBoldDashStyleProperty,
            WickThicknessProperty,
            WickColorProperty,
            WickDashStyleProperty,
            CandleOutlineThicknessProperty,
            CandleOutlineDashStyleProperty,
            CandleOutlineColorProperty,
            CrosshairLineColorProperty,
            CrosshairLineThicknessProperty,
            CrosshairLineDashStyleProperty
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
        SizeChanged += PanelSizeChanged;

        ContextMenu = ChartPanelContextMenu.CreateDefault();
    }

    ~ChartPanel()
    {
        PointerWheelChanged -= OnPointerWheelChanged;
        PointerPressed -= OnPointerPressed;
        PointerMoved -= OnPointerMoved;
        PointerReleased -= OnPointerReleased;
        SizeChanged -= PanelSizeChanged;

        if (_vm != null)
        {
            _vm.Data.CollectionChanged -= DataCollectionChanged;
            if (_vm.XAxis != null) _vm.XAxis.PropertyChanged -= AxisPropertyChanged;
            _vm.YAxis.PropertyChanged -= AxisPropertyChanged;
        }
    }

    internal int PanelNumber { set; get; } = 0;
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
            _vm.Data.CollectionChanged -= DataCollectionChanged;
            if (_vm.XAxis != null) _vm.XAxis.PropertyChanged -= AxisPropertyChanged;
            _vm.YAxis.PropertyChanged -= AxisPropertyChanged;
        }
        
        _vm = DataContext as ChartPanelViewModel;

        if (_vm != null)
        {
            _vm.Data.CollectionChanged += DataCollectionChanged;
            if (_vm.XAxis != null) _vm.XAxis.PropertyChanged += AxisPropertyChanged;
            _vm.YAxis.PropertyChanged += AxisPropertyChanged;
        }
    }

    private void DataCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        _visibleBarDataPoints.Clear();
        //UpdateAxisRanges();
        UpdateVisibleRange();
    }

    private void PanelSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        _visibleBarDataPoints.Clear();
        UpdateVisibleRange();
    }

    //////////
    //Replaced by UpdateVisibleRange()
    //////////
    //private void UpdateAxisRanges()
    //{
    //    if(_vm == null || _vm.XAxis==null) return;

    //    if (_vm.Data.Count == 0)
    //    {
    //        // Default empty view
    //        _vm.XAxis.Min = DateTime.Today;
    //        _vm.XAxis.Max = DateTime.Today.AddDays(1);
    //        _vm.YAxis.Min = 0;
    //        _vm.YAxis.Max = 100;
    //        return;
    //    }

    //    DateTime xMin = DateTime.MaxValue;
    //    DateTime xMax = DateTime.MinValue;
    //    double yMin = double.MaxValue;
    //    double yMax = double.MinValue;

    //    foreach (BarDataSeries series in _vm.Data)
    //    {
    //        // X Axis: Time range
    //        var minTime = series.Min(b => b.Time);
    //        var maxTime = series.Max(b => b.Time);

    //        // Add small padding (5% of range or 1 hour, whichever is larger)
    //        var timePadding = TimeSpan.FromTicks(Math.Max(
    //            (maxTime - minTime).Ticks / 20,
    //            TimeSpan.FromHours(1).Ticks));

    //        DateTime n = minTime - timePadding;
    //        xMin = xMin < n ? xMin : n;

    //        n = maxTime + timePadding;
    //        xMax = xMax > n ? xMax : n;

    //        // Y Axis: Price range (use High/Low for candlesticks)
    //        var minPrice = series.Min(b => b.Low);
    //        var maxPrice = series.Max(b => b.High);

    //        var priceRange = maxPrice - minPrice;
    //        var pricePadding = priceRange * 0.1; // 10% padding
    //        if (pricePadding < 0.01) pricePadding = 1; // Minimum padding

    //        double m = minPrice - pricePadding;
    //        yMin = yMin < m ? yMin : m;

    //        m = maxPrice + pricePadding;
    //        yMax = yMax > m ? yMax : m;
    //    }

    //    _vm.XAxis.Min = xMin;
    //    _vm.XAxis.Max = xMax;
    //    _vm.YAxis.Min = yMin;
    //    _vm.YAxis.Max = yMax;
    //}

    private void AxisPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _visibleBarDataPoints.Clear();
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

    private string _nearestPriceLabel = string.Empty;
    private struct PriceCoordPair
    {
        public PriceCoordPair(double price,double yCoord,string label) { YCoord = yCoord;Price = price; Label = label; }
        public double Price;
        public double YCoord;
        public string Label;
    }

    private DateTime _lastPointerMovedInvalidateVisual = DateTime.MinValue;
    private const double _pointerMoveDebounceMs = 16;

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_vm == null || _vm.XAxis == null)
        {
            e.Handled = true;
            return;
        }

        var currentPos = e.GetPosition(this);

        if (_isDragging)
        {
            _vm.CrosshairTime = null;
            _vm.CrosshairPrice = null;
            _nearestPriceLabel = string.Empty;

            var delta = currentPos - _dragStart; // delta.X and delta.Y in pixels

            if (Math.Abs(delta.X) > 0.5) // avoid jitter
            {
                TimeSpan currentSpan = _dragStartXMax - _dragStartXMin;
                if (currentSpan > TimeSpan.Zero)
                {
                    double totalMs = currentSpan.TotalMilliseconds;
                    double fractionShift = delta.X / Bounds.Width;  // -1.0 to 1.0 for full drag left/right
                    double msToShift = fractionShift * totalMs * PanSensitivity * -1;  // Negative for natural direction (drag right to pan left)

                    long ticksToShift = (long)(msToShift * TimeSpan.TicksPerMillisecond);

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
                    double unitsToShift = delta.Y * pixelsPerUnit * ScrollSensitivity; // positive delta.Y = drag down to view moves down

                    _vm.YAxis.Min = _dragStartYMin + unitsToShift;
                    _vm.YAxis.Max = _dragStartYMax + unitsToShift;
                }
            }
        }
        else
        {
            _vm.MousePosition = currentPos;
            _nearestPriceLabel = string.Empty;

            if (_vm.YAxis != null)
            {
                if (CrosshairSnapMode == CrosshairSnapMode.Free || _vm.Data.Count == 0 || IsSubPanel)
                {
                    double xFraction = currentPos.X / Bounds.Width;
                    TimeSpan span = _vm.XAxis.Max - _vm.XAxis.Min;
                    _vm.CrosshairTime = _vm.XAxis.Min + TimeSpan.FromTicks((long)(xFraction * span.Ticks));

                    double yFraction = 1.0 - (currentPos.Y / Bounds.Height); // Invert Y
                    double range = _vm.YAxis.Max - _vm.YAxis.Min;
                    _vm.CrosshairPrice = _vm.YAxis.Min + yFraction * range;
                }
                else if (CrosshairSnapMode == CrosshairSnapMode.NearestBarPrice)
                {
                    double xFraction = currentPos.X / Bounds.Width;
                    TimeSpan totalSpan = _vm.XAxis.Max - _vm.XAxis.Min;
                    DateTime mouseTime = _vm.XAxis.Min + TimeSpan.FromTicks((long)(xFraction * totalSpan.Ticks));

                    if (_vm.Data.Count != _visibleBarDataPoints.Count)
                    {
                        CalculateVisibleBarDataPoints();
                    }

                    List<IDataPoint> nearestPoints = new List<IDataPoint>();
                    foreach (List<IDataPoint> visibleData in _visibleBarDataPoints)
                    {
                        if (visibleData.Count == 0) continue;

                        var nearestPoint = visibleData
                            .OrderBy(b => Math.Abs((b.X - mouseTime).Ticks))
                            .First();
                        nearestPoints.Add(nearestPoint);
                    }

                    if (nearestPoints.Count == 0)
                    {
                        _vm.CrosshairTime = null;
                        _vm.CrosshairPrice = null;
                        InvalidateVisual();
                        e.Handled = true;
                        return;
                    }

                    TimeDataBar? nearestBar = nearestPoints
                             .OrderBy(b => Math.Abs((b.X - mouseTime).Ticks))
                             .First() as TimeDataBar;

                    if (nearestBar != null)
                    {
                        _vm.CrosshairTime = nearestBar.Time;

                        //..get y coords for each price..then order by and pick first
                        List<PriceCoordPair> yPairs = new List<PriceCoordPair>();

                        yPairs.Add(new PriceCoordPair(nearestBar.Open, ChartPanel.MapYToScreen(_vm.YAxis, nearestBar.Open, Bounds), "O:"));
                        yPairs.Add(new PriceCoordPair(nearestBar.High, ChartPanel.MapYToScreen(_vm.YAxis, nearestBar.High, Bounds), "H:"));
                        yPairs.Add(new PriceCoordPair(nearestBar.Low, ChartPanel.MapYToScreen(_vm.YAxis, nearestBar.Low, Bounds), "L:"));
                        yPairs.Add(new PriceCoordPair(nearestBar.Close, ChartPanel.MapYToScreen(_vm.YAxis, nearestBar.Close, Bounds), "C:"));

                        PriceCoordPair nearestPair = yPairs
                             .OrderBy(b => Math.Abs((b.YCoord - currentPos.Y)))
                             .First();

                        _vm.CrosshairPrice = nearestPair.Price;
                        _nearestPriceLabel = nearestPair.Label;
                    }
                    else
                    {
                        _vm.CrosshairTime = null;
                        _vm.CrosshairPrice = null;
                    }

                    // Optional: Store full OHLC for readout
                    //_vm.CrosshairHigh = nearestBar.High;
                    //_vm.CrosshairLow = nearestBar.Low;
                    //_vm.CrosshairOpen = nearestBar.Open;

                }
            }
        }

        //if ((DateTime.Now - _lastPointerMovedInvalidateVisual).TotalMilliseconds > _pointerMoveDebounceMs)
        //{
        InvalidateVisual();  // Immediate redraw
        //   _lastPointerMovedInvalidateVisual = DateTime.Now;
        //}
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

        double calcY = worldY;// Y < bounds.Height ? worldY : bounds.Height;

        return bounds.Height - ((calcY - vm.Min) / range * bounds.Height);  // y=0 at bottom
    }

    public static bool IsMajorTick(DateTime dt) => dt.TimeOfDay == TimeSpan.Zero || dt.Hour % 6 == 0;

    private static DateTime AlignToInterval(DateTime dt, DataInterval interval)
    {
        return interval.Type switch
        {
            Interval.Second => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, (dt.Second / 10) * 10),
            Interval.Minute => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, (dt.Minute / 5) * 5, 0),
            Interval.Hour => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0),
            Interval.Day => new DateTime(dt.Year, dt.Month, dt.Day),
            Interval.Week => dt.Date.AddDays(-(int)dt.DayOfWeek), // Monday start
            Interval.Month => new DateTime(dt.Year, dt.Month, 1),
            _ => dt.Date
        };
    }
    public static List<DateTime> ComputeDateTimeTicks(DateTime min, DateTime max, Rect bounds, DataInterval interval)
    {
        if (min >= max) return new List<DateTime>();

        // Determine base tick interval from data interval
        TimeSpan baseInterval = interval.Type switch
        {
            Interval.Second => TimeSpan.FromSeconds(Math.Max(10, interval.Value * 6)), // e.g., 1s bars to ticks every 10s
            Interval.Minute => TimeSpan.FromMinutes(Math.Max(1, interval.Value * 5)),   // 1m to every 5m, 5m to every 25m
            Interval.Hour => TimeSpan.FromHours(Math.Max(1, interval.Value * 4)),
            Interval.Day => TimeSpan.FromDays(Math.Max(1, interval.Value * 7)),      // 1d to weekly ticks
            Interval.Week => TimeSpan.FromDays(30),                                  // Weekly to monthly
            Interval.Month => TimeSpan.FromDays(90),                                  // Monthly to quarterly
            Interval.Year => TimeSpan.FromDays(365*10),                                  // Yearly to decade
            _ => TimeSpan.FromDays(1)
        };

        // Cap number of ticks to avoid overcrowding (~10–20 max)
        TimeSpan visibleSpan = max - min;
        int estimatedTicks = (int)(visibleSpan / baseInterval);
        if (estimatedTicks > 20)
        {
            // Increase interval to reduce density
            baseInterval = TimeSpan.FromTicks(baseInterval.Ticks * (estimatedTicks / 15 + 1));
        }
        else if (estimatedTicks < 5 && baseInterval > TimeSpan.FromMinutes(1))
        {
            // Decrease if too sparse
            baseInterval = TimeSpan.FromTicks(baseInterval.Ticks / 2);
        }

        // Align start to a "nice" boundary based on interval type
        DateTime start = AlignToInterval(min, interval);

        var ticks = new List<DateTime>();
        for (DateTime t = start; t <= max; t += baseInterval)
        {
            ticks.Add(t);
        }

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


    public static readonly StyledProperty<double> PrefererredCandleWidthProperty =
    AvaloniaProperty.Register<ChartPanel, double>(nameof(PrefererredCandleWidth), 5);
    public double PrefererredCandleWidth
    {
        get { return GetValue(PrefererredCandleWidthProperty); }
        set {
            double v = Math.Clamp(value, 3, 31);
            SetValue(PrefererredCandleWidthProperty, value);
        }
    }

    #region CandleOutline pen properties
    private Pen? _cachedCandleOutlinePen;

    public static readonly StyledProperty<IBrush> CandleOutlineColorProperty =
    AvaloniaProperty.Register<ChartPanel, IBrush>(nameof(CandleOutlineColor), Brushes.DarkGray);
    public IBrush CandleOutlineColor
    {
        get { return GetValue(CandleOutlineColorProperty); }
        set { SetValue(CandleOutlineColorProperty, value); }
    }

    public static readonly StyledProperty<double> CandleOutlineThicknessProperty =
    AvaloniaProperty.Register<ChartPanel, double>(nameof(CandleOutlineThickness), 1);
    public double CandleOutlineThickness
    {
        get { return GetValue(CandleOutlineThicknessProperty); }
        set { SetValue(CandleOutlineThicknessProperty, value); }
    }

    public static readonly StyledProperty<IDashStyle?> CandleOutlineDashStyleProperty =
    AvaloniaProperty.Register<ChartPanel, IDashStyle?>(nameof(CandleOutlineDashStyle), null);
    public IDashStyle? CandleOutlineDashStyle
    {
        get { return GetValue(CandleOutlineDashStyleProperty); }
        set { SetValue(CandleOutlineDashStyleProperty, value); }
    }
    #endregion

    #region Wick pen properties
    private Pen? _cachedWickPen;

    public static readonly StyledProperty<IBrush> WickColorProperty =
    AvaloniaProperty.Register<ChartPanel, IBrush>(nameof(WickColor), Brushes.DarkGray);
    public IBrush WickColor
    {
        get { return GetValue(WickColorProperty); }
        set { SetValue(WickColorProperty, value); }
    }

    public static readonly StyledProperty<double> WickThicknessProperty =
    AvaloniaProperty.Register<ChartPanel, double>(nameof(WickThickness), 1);
    public double WickThickness
    {
        get { return GetValue(WickThicknessProperty); }
        set { SetValue(WickThicknessProperty, value); }
    }

    public static readonly StyledProperty<IDashStyle?> WickDashStyleProperty =
    AvaloniaProperty.Register<ChartPanel, IDashStyle?>(nameof(WickDashStyle), null);
    public IDashStyle? WickDashStyle
    {
        get { return GetValue(WickDashStyleProperty); }
        set { SetValue(WickDashStyleProperty, value); }
    }
    #endregion

    #region CandleUpColor property
    public static readonly StyledProperty<IBrush> CandleUpColorProperty =
        AvaloniaProperty.Register<ChartPanel, IBrush>(nameof(CandleUpColor), Brushes.Green);
    public IBrush CandleUpColor
    {
        get { return GetValue(CandleUpColorProperty); }
        set { SetValue(CandleUpColorProperty, value); }
    }
    #endregion

    #region CandleDownColor property
    public static readonly StyledProperty<IBrush> CandleDownColorProperty =
        AvaloniaProperty.Register<ChartPanel, IBrush>(nameof(CandleDownColor), Brushes.Red);
    public IBrush CandleDownColor
    {
        get { return GetValue(CandleDownColorProperty); }
        set { SetValue(CandleDownColorProperty, value); }
    }
    #endregion

    #region ScrollSensitivity property
    public static readonly StyledProperty<double> ScrollSensitivityProperty =
        AvaloniaProperty.Register<ChartPanel, double>(nameof(ScrollSensitivity), .25);
    public double ScrollSensitivity
    {
        get { return GetValue(ScrollSensitivityProperty); }
        set
        {
            double v = Math.Clamp(value, 0.1, 5.0);
            SetValue(ScrollSensitivityProperty, v);
        }
    }
    #endregion

    #region PanSensitivity property
    public static readonly StyledProperty<double> PanSensitivityProperty =
        AvaloniaProperty.Register<ChartPanel, double>(nameof(PanSensitivity), 1);
    public double PanSensitivity
    {
        get { return GetValue(PanSensitivityProperty); }
        set
        {
            double v = Math.Clamp(value, 0.1, 5.0);
            SetValue(PanSensitivityProperty, v);
        }
    }
    #endregion

    #region ShowGridLines property
    public static readonly StyledProperty<bool> ShowGridLinesProperty =
        AvaloniaProperty.Register<ChartPanel, bool>(nameof(ShowGridLines), true);
    public bool ShowGridLines
    {
        get { return GetValue(ShowGridLinesProperty); }
        set { SetValue(ShowGridLinesProperty, value); }
    }
    #endregion

    #region ShowCrosshair property
    public static readonly StyledProperty<bool> ShowCrosshairProperty =
        AvaloniaProperty.Register<ChartPanel, bool>(nameof(ShowCrosshair), true);
    public bool ShowCrosshair
    {
        get { return GetValue(ShowCrosshairProperty); }
        set { SetValue(ShowCrosshairProperty, value); }
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

    #region Crosshair properties
    #region CrosshairSnapMode property
    public static readonly StyledProperty<CrosshairSnapMode> CrosshairSnapModeProperty =
        AvaloniaProperty.Register<ChartPanel, CrosshairSnapMode>(nameof(CrosshairSnapMode), CrosshairSnapMode.NearestBarPrice);
    public CrosshairSnapMode CrosshairSnapMode
    {
        get { return GetValue(CrosshairSnapModeProperty); }
        set { SetValue(CrosshairSnapModeProperty, value); }
    }
    #endregion

    #region Crosshair pen properties
    Pen? _cachedCrosshairPen;

    public static readonly StyledProperty<IBrush> CrosshairLineColorProperty =
    AvaloniaProperty.Register<ChartPanel, IBrush>(nameof(CrosshairLineColor), Brushes.Gray);
    public IBrush CrosshairLineColor
    {
        get { return GetValue(CrosshairLineColorProperty); }
        set { SetValue(CrosshairLineColorProperty, value); }
    }

    public static readonly StyledProperty<double> CrosshairLineThicknessProperty =
    AvaloniaProperty.Register<ChartPanel, double>(nameof(CrosshairLineThickness), 1);
    public double CrosshairLineThickness
    {
        get { return GetValue(CrosshairLineThicknessProperty); }
        set { SetValue(CrosshairLineThicknessProperty, value); }
    }

    public static readonly StyledProperty<IDashStyle?> CrosshairLineDashStyleProperty =
    AvaloniaProperty.Register<ChartPanel, IDashStyle?>(nameof(CrosshairLineDashStyle), null);
    public IDashStyle? CrosshairLineDashStyle
    {
        get { return GetValue(CrosshairLineDashStyleProperty); }
        set { SetValue(CrosshairLineDashStyleProperty, value); }
    }
    #endregion

    #region Crosshair readout properties
    #region CrosshairReadoutForegroundColor property
    public static readonly StyledProperty<IBrush> CrosshairReadoutForegroundColorProperty =
        AvaloniaProperty.Register<ChartPanel, IBrush>(nameof(CrosshairReadoutForegroundColor), Brushes.White);
    public IBrush CrosshairReadoutForegroundColor
    {
        get { return GetValue(CrosshairReadoutForegroundColorProperty); }
        set { SetValue(CrosshairReadoutForegroundColorProperty, value); }
    }
    #endregion

    #region CrosshairReadoutBackgroundColor property
    public static readonly StyledProperty<IBrush> CrosshairReadoutBackgroundColorProperty =
        AvaloniaProperty.Register<ChartPanel, IBrush>(nameof(CrosshairReadoutBackgroundColor), Brushes.Black);
    public IBrush CrosshairReadoutBackgroundColor
    {
        get { return GetValue(CrosshairReadoutBackgroundColorProperty); }
        set { SetValue(CrosshairReadoutBackgroundColorProperty, value); }
    }
    #endregion
    
    #region CrosshairReadoutFontSize property
    public static readonly StyledProperty<int> CrosshairReadoutFontSizeProperty =
        AvaloniaProperty.Register<ChartPanel, int>(nameof(CrosshairReadoutFontSize), 12);
    public int CrosshairReadoutFontSize
    {
        get { return GetValue(CrosshairReadoutFontSizeProperty); }
        set { SetValue(CrosshairReadoutFontSizeProperty, value); }
    }
    #endregion

    Typeface _crosshairReadoutTypeface = new Typeface("Consolas");
    #endregion
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
        _cachedWickPen = null;
        _cachedCandleOutlinePen = null;
        _cachedCrosshairPen = null;
    }

    List<ChartComponentBase> _attachedComponents = new List<ChartComponentBase>();
    List<List<IDataPoint>> _visibleBarDataPoints = new List<List<IDataPoint>>();

    internal bool IsSubPanel { get; set; } = false;

    internal void AttachChartComponent(ChartComponentBase component)
    {
        if (_vm == null) return;
        _attachedComponents.Add(component);
        _vm.ChartComponents.Add(component.Properties);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        using (DrawingContext.PushedState clipState = context.PushClip(new Rect(Bounds.Size)))
        {
            DrawBackground(context);
            if (ShowGridLines) DrawGridLines(context);
            if (!IsSubPanel) DrawCandlesticks(context);

            if (_vm == null) return;

            IOrderedEnumerable<ChartComponentBase> orderedComponents = _attachedComponents.OrderBy(r => r.RenderOrder);
            foreach (ChartComponentBase component in orderedComponents)
            {
                component.Render(context);
            }

            DrawCrosshair(context);
        }
    }

    private void DrawBackground(DrawingContext context)
    {
        context.FillRectangle(BackgroundColor, new Rect(0, 0, Bounds.Width, Bounds.Height));
    }

    private void DrawGridLines(DrawingContext context)
    {
        if (_vm == null || _vm.XAxis == null || !_vm.ShowGridLines) return;

        _cachedGridLinesPen ??= new Pen(GridLinesColor, GridLinesThickness, GridLinesDashStyle);
        _cachedGridLinesBoldPen ??= new Pen(GridLinesBoldColor, GridLinesBoldThickness, GridLinesBoldDashStyle);

        DataInterval dataInterval = _vm.Data.Count > 0 ? _vm.Data[0].Interval : new DataInterval(Interval.Hour, 2);

        var xTicks = ComputeDateTimeTicks(_vm.XAxis.Min, _vm.XAxis.Max, Bounds, dataInterval);
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

    private void CalculateVisibleBarDataPoints()
    {
        if (_vm == null || _vm.XAxis == null || _vm.Data.Count==0) return;

        _visibleBarDataPoints.Clear();

        for (int i = 0; i < _vm.Data.Count; i++)
        {
            IEnumerable<IDataPoint> v = _vm.Data[i].Where(p => p.X >= _vm.XAxis.Min && p.X <= _vm.XAxis.Max);
            _visibleBarDataPoints.Add(v.ToList());
        }
    }

    private void UpdateVisibleRange()
    {
        if (_vm == null) return;
        
        if (_vm.Data.Count == 0)
        {
            if (_vm.XAxis != null)
            {
                _vm.XAxis.Min = DateTime.Today;
                _vm.XAxis.Max = DateTime.Today.AddDays(1);
            }

            _vm.YAxis.Min = 0;
            _vm.YAxis.Max = 100;
            return;
        }

        if (_vm.XAxis == null) return;

        _visibleBarDataPoints.Clear();

        var bars = _vm.Data[0];

        double preferredWidth = _vm.PreferredCandleWidth;
        int maxVisible = (int)(Bounds.Width / preferredWidth);
        maxVisible = Math.Max(maxVisible, 50);  // Minimum to avoid too-narrow views

        IEnumerable<TimeDataBar> visibleBars;
        if (bars.Count <= maxVisible)
        {
            visibleBars = bars.Tolist();
        }
        else
        {
            visibleBars = bars.TakeLast(maxVisible);  // Last N bars (most recent)
        }

        // X Range
        var minTime = visibleBars.Min(b => b.Time);
        var maxTime = visibleBars.Max(b => b.Time);
        var timeRange = maxTime - minTime;
        var timePadding = timeRange * 0.05;  // 5% padding

        _vm.XAxis.Min = minTime - timePadding;
        _vm.XAxis.Max = maxTime + timePadding;


        // Y Range (use visible only)
        var minY = visibleBars.Min(b => b.Low);
        var maxY = visibleBars.Max(b => b.High);

        List<TimeDataBar> vBars = visibleBars.ToList();
        foreach (ChartComponentBase component in _attachedComponents)
        {
            double componentMinY = component.MinY(vBars[0].Time, vBars[vBars.Count - 1].Time);
            double componentMaxY = component.MaxY(vBars[0].Time, vBars[vBars.Count - 1].Time);

            minY = componentMinY < minY ? componentMinY : minY;
            maxY = componentMaxY > maxY ? componentMaxY : maxY;
        }
        
        var yRange = maxY - minY;
        var yPadding = yRange * 0.1;  // 10% padding
        if (yPadding < 0.01) yPadding = 1;

        _vm.YAxis.Min = minY - yPadding;
        _vm.YAxis.Max = maxY + yPadding;
    }

    private void DrawCandlesticks(DrawingContext context)
    {
        if (_vm == null) return;
        if (_vm.Data.Count == 0) return;
        if (_vm.XAxis is not { } xAxis || _vm.YAxis is not { } yAxis) return;

        TimeSpan xSpan = xAxis.Max - xAxis.Min;
        double yRange = yAxis.Max - yAxis.Min;
        if (xSpan <= TimeSpan.Zero || yRange <= 0) return;
        double pixelsPerTick = Bounds.Width / xSpan.Ticks;

        if (_vm.Data.Count != _visibleBarDataPoints.Count)
        {
            CalculateVisibleBarDataPoints();
        }

        for(int i=0; i <_visibleBarDataPoints.Count;  i++)
        {
            double halfBarWidth = Math.Max(2, Math.Min(12, pixelsPerTick * BarDataSeries.IntervalTicks(_vm.Data[i]) / 2)); // auto-scale width

            foreach (var dataPoint in _visibleBarDataPoints[i])
            {
                TimeDataBar? bar = dataPoint as TimeDataBar;
                if(bar == null) continue;
                
                if (bar.Time < xAxis.Min || bar.Time > xAxis.Max) continue;

                double xCenter = (bar.Time - xAxis.Min).Ticks * pixelsPerTick;

                double highY = Bounds.Height - (bar.High - yAxis.Min) / yRange * Bounds.Height;
                double lowY = Bounds.Height - (bar.Low - yAxis.Min) / yRange * Bounds.Height;
                double openY = Bounds.Height - (bar.Open - yAxis.Min) / yRange * Bounds.Height;
                double closeY = Bounds.Height - (bar.Close - yAxis.Min) / yRange * Bounds.Height;

                bool isBull = bar.Close >= bar.Open;
                var bodyBrush = isBull ? CandleUpColor : CandleDownColor;
                _cachedWickPen ??= new Pen(WickColor, WickThickness, WickDashStyle);
                _cachedCandleOutlinePen ??= new Pen(CandleOutlineColor, CandleOutlineThickness, CandleOutlineDashStyle);

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

    private void DrawCrosshair(DrawingContext context)
    {
        if (_vm == null || !_vm.ShowCrosshair || !_vm.MousePosition.HasValue || !_vm.CrosshairTime.HasValue || !_vm.CrosshairPrice.HasValue || _vm.XAxis == null)
            return;

        Point pos = _vm.MousePosition.Value;

        _cachedCrosshairPen ??= new Pen(CrosshairLineColor, CrosshairLineThickness, CrosshairLineDashStyle);

        // Vertical line
        double snappedX = _vm.CrosshairTime.HasValue
            ? ChartPanel.MapXToScreen(_vm.XAxis, _vm.CrosshairTime.Value, Bounds)
            : pos.X;
        context.DrawLine(_cachedCrosshairPen, new Point(snappedX, 0), new Point(snappedX, Bounds.Height));

        // Horizontal line
        double snappedY = _vm.CrosshairPrice.HasValue
            ? ChartPanel.MapYToScreen(_vm.YAxis, _vm.CrosshairPrice.Value, Bounds)
            : pos.Y;
        context.DrawLine(_cachedCrosshairPen, new Point(0, snappedY), new Point(Bounds.Width, snappedY));


        var sb = new StringBuilder();
        sb.AppendLine(_vm.CrosshairTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));

        if (_vm.CrosshairPrice.HasValue)
            sb.AppendLine($"{_nearestPriceLabel} {_vm.CrosshairPrice.Value:F5}");

        var fullText = sb.ToString();

        var formatted = new FormattedText(
            fullText,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            _crosshairReadoutTypeface,
            CrosshairReadoutFontSize,
            CrosshairReadoutForegroundColor);

        double readoutX = Math.Min(snappedX + 10, Bounds.Width - formatted.Width - 10);
        double readoutY = Math.Max(pos.Y - formatted.Height - 10, 10);

        var readoutRect = new Rect(readoutX - 5, readoutY - 5,
            formatted.Width + 10, formatted.Height + 10);

        context.FillRectangle(CrosshairReadoutBackgroundColor, readoutRect);
        context.DrawRectangle(_cachedCrosshairPen, readoutRect);
        context.DrawText(formatted, new Point(readoutX, readoutY));
    }
}