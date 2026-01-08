using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using EvolverCore.ViewModels;
using EvolverCore.Views;
using EvolverCore.Views.ContextMenus;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace EvolverCore;

public partial class ChartXAxis : Decorator
{
    static ChartXAxis()
    {
        AffectsRender<ChartYAxis>(
            BackgroundColorProperty,
            LabelColorProperty,
            FontSizeProperty
        );
    }

    public ChartXAxis()
    {
        InitializeComponent();
        ContextMenu = ChartXAxisContextMenu.CreateDefault();
    }

    private ChartControlViewModel? _vm;
    private Typeface _typeface = new Typeface("Consolas");

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

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_vm != null && _vm.SharedXAxis != null)
        {
            _vm.SharedXAxis.PropertyChanged -= AxisPropertyChanged;
        }

        _vm = DataContext as ChartControlViewModel;

        if (_vm != null && _vm.SharedXAxis != null)
        {
            _vm.SharedXAxis.PropertyChanged += AxisPropertyChanged;
        }
    }

    private void AxisPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        using (DrawingContext.PushedState clipState = context.PushClip(new Rect(Bounds.Size)))
        {
            DrawBackground(context);
            DrawXAxisLabels(context);
        }
    }

    private void DrawBackground(DrawingContext context)
    {
        context.FillRectangle(BackgroundColor, new Rect(0, 0, Bounds.Width, Bounds.Height));
    }

    public ChartPanel? DataPanel { get; internal set; }

    private void DrawXAxisLabels(DrawingContext context)
    {
        if (_vm == null || _vm.SharedXAxis == null || DataPanel == null) return;

        DataComponent? dataComponent = DataPanel.GetFirstDataComponent();
        IndicatorViewModel? ivm = dataComponent?.Properties as IndicatorViewModel;
        DataInterval dataInterval;
        if (dataComponent == null || ivm == null || ivm.Indicator == null)
            dataInterval = new DataInterval(Interval.Hour, 2);
        else
            dataInterval = ivm.Indicator.Interval;

        List<DateTime> ticks = ChartPanel.ComputeDateTimeTicks(_vm.SharedXAxis.Min, _vm.SharedXAxis.Max, Bounds, dataInterval);

        for (int i = 1; i <= ticks.Count; i++)
        {
            DateTime tick = ticks[i - 1];
            double x = ChartPanel.MapXToScreen(_vm.SharedXAxis, tick, Bounds);
            string label = string.Empty;

            switch (dataInterval.Type)
            {
                case Interval.Second: label = tick.ToString("HH:mm:ss"); break;
                case Interval.Minute: label = tick.ToString("HH:mm"); break;
                case Interval.Hour: label = (tick.Hour == 0 && tick.Minute == 0) ? tick.ToString("MMM d") : tick.ToString("HH:mm"); break;
                case Interval.Day: label = tick.ToString("MMM"); break;
                case Interval.Month: label = i % 2 == 0 ? tick.ToString("MMM") : tick.ToString("y"); break;
                case Interval.Year: label = tick.ToString("yyyy"); break;
                default: label = tick.ToString("d") + " " + tick.ToString("HH:mm:ss"); break;
            }

            var ft = new FormattedText(label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _typeface, FontSize, LabelColor)
            {
                TextAlignment = TextAlignment.Center
            };

            context.DrawText(ft, new Point(x, 4));
        }
    }
}