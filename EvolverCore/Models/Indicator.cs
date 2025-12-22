using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using EvolverCore.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Models
{
    public enum IndicatorState
    {
        New,
        Startup,
        History,
        Live,
        ShuttingDown,
        ShutDown
    }

    public class PlotProperties
    {
        public BarPriceValue PriceField { get; set; } = BarPriceValue.Close;
        public IBrush? PlotFillBrush { get; set; } = Brushes.Cyan;
        public IBrush? PlotLineBrush { get; set; } = Brushes.Turquoise;
        public double PlotLineThickness { get; set; } = 1.5;
        public IDashStyle? PlotLineStyle { get; set; } = null;
        public PlotStyle Style { get; set; } = PlotStyle.Line;
        public string Name { get; set; } = string.Empty;
    }

    public class OutputPlot
    {
        public PlotProperties? Properties { get; internal set; } = null;
        public TimeDataSeries? Series { get; internal set; } = null;
    }

    public class Indicator
    {
        public Indicator() { }

        public string Name { get; set; } = string.Empty;


        public IndicatorState State { get; internal set; } = IndicatorState.New;

        public List<BarDataSeries> Bars { get; internal set; } = new List<BarDataSeries>();

        public List<TimeDataSeries> Inputs { get; internal set; } = new List<TimeDataSeries>();

        public List<OutputPlot> Outputs { get; internal set; } = new List<OutputPlot>();

        public int CurrentBarIndex { get; internal set; } = -1;

        public int CurrentBarsIndex { get; internal set; } = -1;

        public virtual void OnStateChange()
        { }

        public virtual void OnDataUpdate()
        { }

        public virtual void OnRender()
        { }
    }
}
