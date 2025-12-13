using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using EvolverCore.ViewModels;
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
        _vm = DataContext as ChartControlViewModel;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        DrawBackground(context);
        DrawXAxisLabels(context);
    }

    private void DrawBackground(DrawingContext context)
    {
        context.FillRectangle(BackgroundColor, new Rect(0, 0, Bounds.Width, Bounds.Height));
    }

    private void DrawXAxisLabels(DrawingContext context)
    {
        if (_vm == null || _vm.SharedXAxis == null) return;

        List<DateTime> ticks = ChartPanel.ComputeDateTimeTicks(_vm.SharedXAxis.Min,_vm.SharedXAxis.Max); // expose as method or property

        foreach (DateTime tick in ticks)
        {
            double x = ChartPanel.MapXToScreen(_vm.SharedXAxis,tick,Bounds); // expose as public method
            string label = tick.Hour == 0 && tick.Minute == 0
                ? tick.ToString("HH:mm")
                : tick.ToString("HH:mm:ss");

            var ft = new FormattedText(label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _typeface , FontSize, LabelColor)
            {
                TextAlignment = TextAlignment.Center
            };

            context.DrawText(ft, new Point(x, 4));
        }
    }
}