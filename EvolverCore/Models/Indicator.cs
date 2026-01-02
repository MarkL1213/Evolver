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
        public OutputPlot() { }
        public OutputPlot(PlotProperties properties) { Properties = properties; }
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

        IndicatorDataSlice? _slice;

        internal void SetData(IndicatorDataSlice slice)
        { }

        internal IEnumerable<IDataPoint> SelectInputPointsInRange(DateTime min, DateTime max)
        {
            return new List<TimeDataPoint>();
        }
        internal IEnumerable<IDataPoint> SelectOutputPointsInRange(DateTime min, DateTime max, int plotIndex, bool skipLeadingNaN = false)
        {
            return new List<TimeDataPoint>();
        }

        public DateTime MinTime(int lastCount)
        {
            return _slice != null ? _slice.MinTime(lastCount) : DateTime.Now;
        }
        public DateTime MaxTime(int lastCount)
        {
            return _slice != null ? _slice.MaxTime(lastCount) : DateTime.Now;
        }

        public int InputElementCount()
        {
            if (Inputs.Count > 0) return Inputs[0].Count;
            return 0;
        }
        public int OutputElementCount(int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= Outputs.Count || Outputs[plotIndex].Series == null) return 0;
            return Outputs[plotIndex].Series.Count;
        }

        public DataInterval Interval
        {
            get
            {
                if(Inputs.Count > 0) return Inputs[0].Interval;
                return new DataInterval(EvolverCore.Interval.Hour,1);
            }
        }


        public virtual void ConfigurePlots()
        { }

        public virtual void OnStateChange()
        { }

        public virtual void OnDataUpdate()
        { }

        public virtual void OnRender()
        { }
    }
}
