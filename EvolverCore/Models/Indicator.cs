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

    public class InputIndicator
    {
        public InputIndicator(Indicator indicator, int plotIndex) { Indicator = indicator; PlotIndex = plotIndex; }

        public Indicator Indicator { get; internal set; }
        public int PlotIndex { get; internal set; } = -1;
    }

    public class Indicator
    {
        public Indicator() { }

        public string Name { get; set; } = string.Empty;


        public IndicatorState State { get; internal set; } = IndicatorState.New;



        public int CurrentBarIndex { get; internal set; } = -1;

        public int CurrentBarsIndex { get; internal set; } = -1;

        IndicatorDataSourceRecord? _sourceRecord;
        internal IndicatorDataSourceRecord? SourceRecord { get { return _sourceRecord; } }



        ////////////////
        // these should all map into the slice (maybe even be slice properties that are just wrapped here)
        public List<InstrumentDataSlice> Bars { get; internal set; } = new List<InstrumentDataSlice>();
        public List<InputIndicator> Inputs { get; internal set; } = new List<InputIndicator>();
        public List<OutputPlot> Outputs { get; internal set; } = new List<OutputPlot>();
        //////////////////
        internal void SetData(IndicatorDataSourceRecord sourceRecord)
        {
            _sourceRecord = sourceRecord;
            if (_sourceRecord.SourceBarData != null) { Bars.Add(_sourceRecord.SourceBarData); }
            if (_sourceRecord.SourceIndicator != null) { Inputs.Add(new InputIndicator(_sourceRecord.SourceIndicator,_sourceRecord.SourcePlotIndex)); }
        }

        internal void OnSourceDataLoaded(object? sender, EventArgs e)
        {
            if(_sourceRecord == null) { return; }

            InstrumentDataSlice? sourceInstrumentSlice = sender as InstrumentDataSlice;
            if (sourceInstrumentSlice != null) sourceInstrumentSlice.DataLoaded -= OnSourceDataLoaded;

            if (_sourceRecord.SourceBarData != null) { Bars.Add(_sourceRecord.SourceBarData); }

            Globals.Instance.Log.LogMessage("Indicator data loaded", LogLevel.Info);
        }

        internal IEnumerable<IDataPoint> SelectInputPointsInRange(DateTime min, DateTime max)
        {
            if (_sourceRecord != null)
            {
                if (_sourceRecord.SourceType == CalculationSource.BarData && Bars.Count > 0)
                    return Bars[0].Where(p => p.Time >= min && p.Time <= max);
                else if(_sourceRecord.SourceType == CalculationSource.IndicatorPlot && Inputs.Count > 0)
                    return Inputs[0].Indicator.SelectOutputPointsInRange(min, max, Inputs[0].PlotIndex);
            }

            return Enumerable.Empty<IDataPoint>();
        }
        internal IEnumerable<IDataPoint> SelectOutputPointsInRange(DateTime min, DateTime max, int plotIndex, bool skipLeadingNaN = false)
        {
            if(plotIndex <0 ||  plotIndex >= Outputs.Count) return Enumerable.Empty<IDataPoint>();
            OutputPlot oPLot = Outputs[plotIndex];
            if(oPLot.Series == null) return Enumerable.Empty<IDataPoint>();
            return oPLot.Series.Where(p => p.Time >= min && p.Time <= max);
        }

        public DateTime MinTime(int lastCount)
        {
            if (_sourceRecord == null) return DateTime.MinValue;

            if (_sourceRecord.SourceType == CalculationSource.BarData && Bars.Count > 0)
                return Bars[0].MinTime(lastCount);
            else if (_sourceRecord.SourceType == CalculationSource.IndicatorPlot && Inputs.Count > 0)
                return Inputs[0].Indicator.MinTime(lastCount);
            return DateTime.MinValue;
        }

        public DateTime MaxTime(int lastCount)
        {
            if (_sourceRecord == null) return DateTime.MaxValue;

            if (_sourceRecord.SourceType == CalculationSource.BarData && Bars.Count > 0)
                return Bars[0].MaxTime(lastCount);
            else if (_sourceRecord.SourceType == CalculationSource.IndicatorPlot && Inputs.Count > 0)
                return Inputs[0].Indicator.MaxTime(lastCount);
            return DateTime.MaxValue;
        }

        public int InputElementCount()
        {
            if (_sourceRecord != null)
            {
                if (_sourceRecord.SourceType == CalculationSource.BarData && Bars.Count > 0)
                    return Bars[0].Count;
                else if (_sourceRecord.SourceType == CalculationSource.IndicatorPlot && Inputs.Count > 0)
                    return Inputs[0].Indicator.OutputElementCount(Inputs[0].PlotIndex);
            }
            return 0;
        }

        public int OutputElementCount(int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= Outputs.Count) return 0;
            OutputPlot oPlot = Outputs[plotIndex];
            if (oPlot.Series == null) return 0;
            return oPlot.Series.Count;
        }

        public DataInterval Interval
        {
            get
            {
                if (_sourceRecord != null)
                {
                    if (_sourceRecord.SourceType == CalculationSource.BarData && Bars.Count > 0)
                        return Bars[0].Record.Interval;
                    else if (_sourceRecord.SourceType == CalculationSource.IndicatorPlot && Inputs.Count > 0)
                        return Inputs[0].Indicator.Interval;
                }
                return new DataInterval(EvolverCore.Interval.Hour, 1);
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
