using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using EvolverCore.ViewModels;
using EvolverCore.Views.ContextMenus;
using System;
using System.Globalization;

namespace EvolverCore;

public partial class ChartYAxis : Decorator
{
    static ChartYAxis()
    {
        AffectsRender<ChartYAxis>(
            BackgroundColorProperty,
            LabelColorProperty,
            FontSizeProperty,
            TickLineColorProperty,
            TickLineDashStyleProperty,
            TickLineThicknessProperty
            );

        AvaloniaProperty[] penProperties =
        {
            TickLineColorProperty,
            TickLineThicknessProperty,
            TickLineDashStyleProperty
        };

        foreach (AvaloniaProperty p in penProperties)
            p.Changed.AddClassHandler<ChartYAxis>((c, _) => c.InvalidatePenCache());
    }

    public ChartYAxis()
    {
        InitializeComponent();
        ContextMenu = ChartYAxisContextMenu.CreateDefault();
    }

    private void InvalidatePenCache()
    {
        _cachedTickLinePen = null;
    }

    #region BackgroundColor property
    public static readonly StyledProperty<IBrush> BackgroundColorProperty =
        AvaloniaProperty.Register<ChartPanel, IBrush>(nameof(BackgroundColor), Brushes.Black);
    public IBrush BackgroundColor
    {
        get { return GetValue(BackgroundColorProperty); }
        set { SetValue(BackgroundColorProperty, value); }
    }
    #endregion

    #region LabelColor property
    public static readonly StyledProperty<IBrush> LabelColorProperty =
        AvaloniaProperty.Register<ChartPanel, IBrush>(nameof(LabelColor), Brushes.White);
    public IBrush LabelColor
    {
        get { return GetValue(LabelColorProperty); }
        set { SetValue(LabelColorProperty, value); }
    }
    #endregion

    #region FontSize property
    public static readonly StyledProperty<int> FontSizeProperty =
        AvaloniaProperty.Register<ChartPanel, int>(nameof(FontSize), 12);
    public int FontSize
    {
        get { return GetValue(FontSizeProperty); }
        set { SetValue(FontSizeProperty, value); }
    }
    #endregion

    #region TickLine pen properties
    private Pen? _cachedTickLinePen;

    public static readonly StyledProperty<IBrush> TickLineColorProperty =
    AvaloniaProperty.Register<ChartPanel, IBrush>(nameof(TickLineColor), Brushes.White);
    public IBrush TickLineColor
    {
        get { return GetValue(TickLineColorProperty); }
        set { SetValue(TickLineColorProperty, value); }
    }

    public static readonly StyledProperty<double> TickLineThicknessProperty =
    AvaloniaProperty.Register<ChartPanel, double>(nameof(TickLineThickness), 1);
    public double TickLineThickness
    {
        get { return GetValue(TickLineThicknessProperty); }
        set { SetValue(TickLineThicknessProperty, value); }
    }

    public static readonly StyledProperty<IDashStyle?> TickLineDashStyleProperty =
    AvaloniaProperty.Register<ChartPanel, IDashStyle?>(nameof(TickLineDashStyle), null);
    public IDashStyle? TickLineDashStyle
    {
        get { return GetValue(TickLineDashStyleProperty); }
        set { SetValue(TickLineDashStyleProperty, value); }
    }
    #endregion

    private ChartPanel? _connectedChartPanel;
    private ChartPanelViewModel? _vm;
    private Typeface _typeface = new Typeface("Arial");

    public ChartPanel? ChartPanel { get { return _connectedChartPanel; } }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm != null)
        {
            _vm.YAxis.PropertyChanged -= AxisPropertyChanged;
        }

        _vm = DataContext as ChartPanelViewModel;

        if (_vm != null)
        {
            _vm.YAxis.PropertyChanged += AxisPropertyChanged;
        }
    }

    private void AxisPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    public void SetConnectedChartPanel(ChartPanel connectedChartPanel)
    {
        _connectedChartPanel = connectedChartPanel;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        DrawBackground(context);
        DrawTickLinesAndLabels(context);
    }

    private void DrawTickLinesAndLabels(DrawingContext context)
    { 
        if(_vm == null || _vm.XAxis == null) { return; }

        var yTicks = ChartPanel.ComputeDoubleTicks(_vm.YAxis.Min, _vm.YAxis.Max);
        _cachedTickLinePen ??= new Pen(TickLineColor, TickLineThickness, TickLineDashStyle);

        foreach (var tick in yTicks)
        {
            double screenY = ChartPanel.MapYToScreen(_vm.YAxis, tick, Bounds);
            var label = new FormattedText(tick.ToString("F2"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _typeface, FontSize, LabelColor);
            context.DrawText(label, new Point(5, screenY - 6));  // Offset for centering
            context.DrawLine(_cachedTickLinePen, new Point(0, screenY), new Point(10, screenY));
        }
    }

    private void DrawBackground(DrawingContext context)
    {
        context.FillRectangle(BackgroundColor, new Rect(0, 0, Bounds.Width, Bounds.Height));
    }

}