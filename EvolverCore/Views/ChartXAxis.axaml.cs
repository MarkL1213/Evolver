using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using EvolverCore.ViewModels;
using System;
using System.Globalization;

namespace EvolverCore;

public partial class ChartXAxis : Decorator
{
    public ChartXAxis()
    {
        InitializeComponent();
        _backgroundColor = Brushes.Gray;
    }

    //private ChartControlViewModel? _vm;
    IBrush _backgroundColor;

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        DrawBackground(context);
        //if (_vm == null) { return; }
        DrawXAxisLabels(context);
    }

    private void DrawBackground(DrawingContext context)
    {
        context.FillRectangle(_backgroundColor, new Rect(0, 0, Bounds.Width, Bounds.Height));
    }

    private void DrawXAxisLabels(DrawingContext context)
    {
        var vm = DataContext as ChartControlViewModel;
        if (vm?.SharedXAxis is not { } xAxis) return;

        var ticks = ChartPanel.ComputeDateTimeTicks(vm.SharedXAxis.Min,vm.SharedXAxis.Max); // expose as method or property

        Typeface typeface = new Typeface("Consolas");
        IBrush textBrush = Brushes.White;

        foreach (var tick in ticks)
        {
            double x = ChartPanel.MapXToScreen(vm.SharedXAxis,tick,Bounds); // expose as public method
            string label = tick.Hour == 0 && tick.Minute == 0
                ? tick.ToString("HH:mm")
                : tick.ToString("HH:mm:ss");

            var ft = new FormattedText(label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface , 12, textBrush)
            {
                TextAlignment = TextAlignment.Center
            };

            context.DrawText(ft, new Point(x, 4));
        }
    }
}