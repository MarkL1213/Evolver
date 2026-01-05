using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using EvolverCore.Views;
using System;
using System.Collections;
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

        public double this[int barsAgo]
        {
            get
            {
                if (Series == null || barsAgo > Series.Count)
                    throw new EvolverException("OutputPlot get[barsAgo] out of range.");

                return Series[barsAgo].Value;
            }
            set
            {
                if (Series == null || barsAgo > Series.Count)
                    throw new EvolverException("OutputPlot set[barsAgo] out of range.");

                Series[barsAgo].Value = value;
            }
        }
    }

    public class InputIndicator
    {
        public InputIndicator(Indicator indicator, int plotIndex) { Indicator = indicator; PlotIndex = plotIndex; }

        public Indicator Indicator { get; internal set; }
        public int PlotIndex { get; internal set; } = -1;
    }

    public class BarsPointer
    {
        public BarsPointer(InstrumentDataSlice slice) { Slice = slice;CurrentBar = 0; }
        public InstrumentDataSlice Slice { get; internal set; }
        public int CurrentBar { get; internal set; }

        public InstrumentDataSliceRecord Record { get { return Slice.Record; } }
        public int Count { get { return Slice.Count; } }
        public DataLoadState LoadState { get { return Slice.LoadState; } }
        public DateTime MinTime(int lastCount) { return Slice.MinTime(lastCount); }
        public DateTime MaxTime(int lastCount) { return Slice.MaxTime(lastCount); }

        public IEnumerable<TimeDataBar> Where(Func<TimeDataBar,bool> predicate)
        {
            return Slice.Where(predicate);
        }

        public TimeDataBar GetValueAt(int i)
        {
            return Slice.GetValueAt(i);
        }

        public TimeDataBar this[int barsAgo]
        {
            get
            {
                int n = Slice.StartOffset + CurrentBar - barsAgo;
                return Slice.GetValueAt(n);
            }
            set
            {

                int n = Slice.StartOffset + CurrentBar - barsAgo;
                Slice.SetValueAt(value,n);
            }
        }
    }

    public class Indicator
    {
        public Indicator() { }

        internal event EventHandler? DataChanged;
        public string Name { get; set; } = string.Empty;


        public IndicatorState State { get; internal set; } = IndicatorState.New;



        public int CurrentBarIndex { get; internal set; } = -1;

        public int CurrentBarsIndex { get; internal set; } = -1;

        IndicatorDataSourceRecord? _sourceRecord;
        internal IndicatorDataSourceRecord? SourceRecord { get { return _sourceRecord; } }

        internal bool IsDataOnly { get; set; } = false;


        ////////////////
        // these should all map into the slice (maybe even be slice properties that are just wrapped here)
        public List<BarsPointer> Bars { get; internal set; } = new List<BarsPointer>();
        public List<InputIndicator> Inputs { get; internal set; } = new List<InputIndicator>();
        public List<OutputPlot> Outputs { get; internal set; } = new List<OutputPlot>();
        //////////////////
        internal void SetSourceData(IndicatorDataSourceRecord sourceRecord)
        {
            _sourceRecord = sourceRecord;
            if (_sourceRecord.SourceBarData != null)
            {
                Bars.Add(new BarsPointer(_sourceRecord.SourceBarData));
                if (DataChanged != null) DataChanged(this, EventArgs.Empty);
            }
            if (_sourceRecord.SourceIndicator != null)
            {
                Inputs.Add(new InputIndicator(_sourceRecord.SourceIndicator,_sourceRecord.SourcePlotIndex));
                if (DataChanged != null) DataChanged(this, EventArgs.Empty);
            }

        }

        internal void OnInputDataLoaded(object? sender, EventArgs e)
        {
            //FIXME -- an input added during configure() has been loaded, set it up

            if (!WaitingForDataLoad)
            {
                Globals.Instance.DataManager.IndicatorReadyToRun(this);
            }
        }

        internal void OnSourceDataLoaded(object? sender, EventArgs e)
        {
            if(_sourceRecord == null) { return; }

            InstrumentDataSlice? sourceInstrumentSlice = sender as InstrumentDataSlice;
            if (sourceInstrumentSlice != null) sourceInstrumentSlice.DataLoaded -= OnSourceDataLoaded;

            if (_sourceRecord.SourceBarData != null)
            {
                Bars.Add(new BarsPointer(_sourceRecord.SourceBarData));
                if (DataChanged != null) DataChanged(this, EventArgs.Empty);
            }

            if (!WaitingForDataLoad)
            {
                Globals.Instance.DataManager.IndicatorReadyToRun(this);
            }
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

        public bool WaitingForDataLoad
        {
            get
            {
                foreach (BarsPointer bars in Bars)
                {
                    if (bars.LoadState != DataLoadState.Loaded) return true;
                }
                foreach (InputIndicator input in Inputs)
                {
                    if(input.Indicator.WaitingForDataLoad) return true;
                }

                return false;
            }
        }

        internal void DataUpdate()
        {
            //setup the next value in the output plots
            //set the correct current indexes

            OnDataUpdate();
        }

        internal void Startup()
        {
            State = IndicatorState.Startup;

            Configure();
        }


        public virtual void Configure()
        { }

        public virtual void OnStateChange()
        { }

        public virtual void OnDataUpdate()
        { }

        public virtual void OnRender()
        { }
    }
}
