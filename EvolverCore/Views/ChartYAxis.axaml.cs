using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using EvolverCore.ViewModels;
using System;
using System.Globalization;

namespace EvolverCore;

public partial class ChartYAxis : Decorator
{
    public ChartYAxis()
    {
        InitializeComponent();
        _backgroundColor = Brushes.Gray;
    }

    IBrush _backgroundColor;
    ChartPanel? _connectedChartPanel;
    private ChartPanelViewModel? _vm;

    public ChartPanel? ChartPanel { get { return _connectedChartPanel; } }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _vm = DataContext as ChartPanelViewModel;
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
        if (_vm == null) { return; }

        var yTicks = ChartPanel.ComputeDoubleTicks(_vm.YAxis.Min, _vm.YAxis.Max);
        var textBrush = Brushes.White;
        var typeface = new Typeface("Arial");

        foreach (var tick in yTicks)
        {
            double screenY = ChartPanel.MapYToScreen(_vm.YAxis, tick, Bounds);
            var label = new FormattedText(tick.ToString("F2"), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 12, textBrush);
            context.DrawText(label, new Point(5, screenY - 6));  // Offset for centering
            context.DrawLine(new Pen(textBrush), new Point(0, screenY), new Point(10, screenY));
        }
    }
    private void DrawBackground(DrawingContext context)
    {
        context.FillRectangle(_backgroundColor, new Rect(0, 0, Bounds.Width, Bounds.Height));
    }

}