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
using EvolverCore.Views;
using EvolverCore.Models;
using System.Runtime.InteropServices;


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
            GridLinesBoldDashStyleProperty,
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
        PointerExited += OnPointerExited;
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
            _vm.ChartComponents.CollectionChanged -= ComponentCollectionChanged;
            if (_vm.XAxis != null) _vm.XAxis.PropertyChanged -= AxisPropertyChanged;
            _vm.YAxis.PropertyChanged -= AxisPropertyChanged;
        }
    }

    public int PanelNumber { get; internal set; } = 0;
    private string _nearestPriceLabel = string.Empty;
    private List<ChartComponentBase> _attachedComponents = new List<ChartComponentBase>();
    private Point _dragStart;
    private DateTime _dragStartXMin;
    private DateTime _dragStartXMax;
    private double _dragStartYMin;
    private double _dragStartYMax;
    private bool _isDragging;
    private ChartPanelViewModel? _vm;

    private struct PriceCoordPair
    {
        public PriceCoordPair(double price, double yCoord, string label) { YCoord = yCoord; Price = price; Label = label; }
        public double Price;
        public double YCoord;
        public string Label;
    }

    #region callbacks
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm != null)
        {
            _vm.ChartComponents.CollectionChanged -= ComponentCollectionChanged;
            if (_vm.XAxis != null) _vm.XAxis.PropertyChanged -= AxisPropertyChanged;
            _vm.YAxis.PropertyChanged -= AxisPropertyChanged;
        }
        
        _vm = DataContext as ChartPanelViewModel;

        if (_vm != null)
        {
            _vm.ChartComponents.CollectionChanged += ComponentCollectionChanged;
            if (_vm.XAxis != null) _vm.XAxis.PropertyChanged += AxisPropertyChanged;
            _vm.YAxis.PropertyChanged += AxisPropertyChanged;
        }
    }

    internal void OnDataUpdate()
    {
        UpdateYAxisRange();
        foreach (ChartComponentBase component in _attachedComponents) component.CalculateSnapPoints();
        InvalidateVisual();
    }

    private void ComponentCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        foreach (ChartComponentBase component in _attachedComponents) component.CalculateSnapPoints();
        //UpdateVisibleRange();
    }

    private void PanelSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        foreach (ChartComponentBase component in _attachedComponents) component.CalculateSnapPoints();
        //UpdateVisibleRange();
    }

    private void AxisPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        foreach (ChartComponentBase component in _attachedComponents) component.CalculateSnapPoints();
        InvalidateVisual();
    }
    
    private void InvalidatePenCache()
    {
        _cachedGridLinesPen = null;
        _cachedGridLinesBoldPen = null;
        _cachedCrosshairPen = null;
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

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (_vm != null)
        {
            _vm.CrosshairPrice = null;
            _vm.CrosshairTime = null;
            InvalidateVisual();
        }

        e.Handled = true;
        return;
    }

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
                DataComponent? dataComponent = GetFirstDataComponent();

                if (CrosshairSnapMode == CrosshairSnapMode.Free || _vm.ChartComponents.Count == 0 || dataComponent == null)
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

                    if (dataComponent.SnapPoints.Count == 0)
                        dataComponent.CalculateSnapPoints();
                    if (dataComponent.SnapPoints.Count == 0)
                    {//No visible data, fallback to free mode
                        _vm.CrosshairTime = _vm.XAxis.Min + TimeSpan.FromTicks((long)(xFraction * totalSpan.Ticks));

                        double yFraction = 1.0 - (currentPos.Y / Bounds.Height); // Invert Y
                        double range = _vm.YAxis.Max - _vm.YAxis.Min;
                        _vm.CrosshairPrice = _vm.YAxis.Min + yFraction * range;
                        InvalidateVisual();
                        return;
                    }

                    TimeDataBar? nearestBar = dataComponent.SnapPoints
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
                }
            }
        }

        InvalidateVisual();


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
    #endregion

    #region static coordinate and axis tick helpers
    internal static double MapXToScreen(ChartXAxisViewModel vm,DateTime dateX, Rect bounds)
    {
        if (vm == null) return 0;
        var span = vm.Max - vm.Min;
        if (span <= TimeSpan.Zero) return 0;

        return (dateX - vm.Min).TotalMilliseconds / span.TotalMilliseconds * bounds.Width;
    }

    internal static double MapYToScreen(ChartYAxisViewModel vm,double worldY, Rect bounds)
    {
        if (worldY == double.NaN) return double.NaN;

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
    #endregion

    #region Properties
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
    #endregion
    
    internal void AttachChartComponent(ChartComponentBase component)
    {
        if (_vm == null) return;
        _attachedComponents.Add(component);
        _vm.ChartComponents.Add(component.Properties);
        InvalidateVisual();
    }

    internal void DetachAllChartComponents()
    {
        _attachedComponents.Clear();
        if (_vm != null) _vm.ChartComponents.Clear();
        InvalidateVisual();
    }

    internal bool ContainsIndicator(Indicator indicator)
    {
        foreach (ChartComponentBase component in _attachedComponents)
        {
            if (component is IndicatorComponent)
            {
                IndicatorComponent? indicatorComponent =  component as IndicatorComponent;
                if (indicatorComponent == null) continue;
                if (indicatorComponent.ContainsIndicator(indicator)) return true;
            }
        }

        return false;
    }

    internal DataComponent? GetFirstDataComponent()
    {
        if (_vm == null || _attachedComponents.Count == 0) return null;

        DataComponent? dataComponent = _attachedComponents.FirstOrDefault(p => p is DataComponent) as DataComponent;
        return dataComponent;
    }

    internal void UpdateXAxisRange()
    {
        if (_vm == null || _vm.XAxis == null) return;
        bool useDefaults = false;
        DataComponent? dataComponent = GetFirstDataComponent();
        if (_vm.ChartComponents.Count == 0 || dataComponent == null) useDefaults = true;
        IndicatorViewModel? ivm = dataComponent?.Properties as IndicatorViewModel;
        if(ivm == null || ivm.Indicator == null || ivm.Indicator.InputElementCount() == 0 || ivm.ChartPlots.Count == 0) useDefaults = true;

        DataPlotViewModel? plot = ivm?.ChartPlots[0] as DataPlotViewModel;
        if (plot == null) useDefaults = true;

        if (useDefaults)
        {
            if (_vm.XAxis != null)
            {
                _vm.XAxis.Min = DateTime.Today;
                _vm.XAxis.Max = DateTime.Today.AddDays(1);
            }

            return;
        }

        

        double preferredWidth = plot.PreferredCandleWidth;

        int maxVisible = (int)(Bounds.Width / preferredWidth);
        maxVisible = Math.Max(maxVisible, 50);  // Minimum to avoid too-narrow views

        var minTime = ivm.Indicator.MinTime(maxVisible);
        var maxTime = ivm.Indicator.MaxTime(maxVisible);
        var timeRange = maxTime - minTime;
        var timePadding = timeRange * 0.05;  // 5% padding

        _vm.XAxis.Min = minTime - timePadding;
        _vm.XAxis.Max = maxTime + timePadding;
    }

    public void UpdateYAxisRange()
    {
        if (_vm == null) return;
        if (_vm.XAxis == null)
        {
            _vm.YAxis.Min = 0;
            _vm.YAxis.Max = 100;

            return;
        }

        // Y Range (use visible only)
        var minY = double.MaxValue;
        var maxY = double.MinValue;

        foreach (ChartComponentBase component in _attachedComponents)
        {
            component.UpdateVisualRange(_vm.XAxis.Min, _vm.XAxis.Max);
            double componentMinY = component.MinY();
            double componentMaxY = component.MaxY();

            minY = componentMinY < minY ? componentMinY : minY;
            maxY = componentMaxY > maxY ? componentMaxY : maxY;
        }

        var yRange = maxY - minY;
        var yPadding = yRange * 0.1;  // 10% padding
        if (yPadding < 0.01) yPadding = 1;

        _vm.YAxis.Min = minY - yPadding;
        _vm.YAxis.Max = maxY + yPadding;
    }

    #region render functions
    public override void Render(DrawingContext context)
    {
        base.Render(context);

        using (DrawingContext.PushedState clipState = context.PushClip(new Rect(Bounds.Size)))
        {
            DrawBackground(context);
            if (ShowGridLines) DrawGridLines(context);
            //if (!IsSubPanel) DrawCandlesticks(context);

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

        DataComponent? dataComponent = GetFirstDataComponent();
        IndicatorViewModel? ivm = dataComponent?.Properties as IndicatorViewModel;
        DataInterval dataInterval;
        if (dataComponent == null || ivm==null || ivm.Indicator == null || ivm.Indicator.InputElementCount() == 0)
            dataInterval = new DataInterval(Interval.Hour, 2);
        else
            dataInterval = ivm.Indicator.Interval;

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
    #endregion
}