using Avalonia.Media;
using Avalonia.Media.TextFormatting.Unicode;
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

        
        public string Name { get; set; } = string.Empty;

        public PlotProperties() { }
        public PlotProperties(PlotProperties source)
        {
            PriceField = source.PriceField;
            PlotLineThickness = source.PlotLineThickness;
            Name = source.Name;
            PlotFillBrush = SerializableBrush.CopyBrush(source.PlotFillBrush);
            PlotLineBrush = SerializableBrush.CopyBrush(source.PlotLineBrush);
            PlotLineStyle = SerializableDashStyle.CopyStyle(source.PlotLineStyle);
        }
    }

    public class OutputPlot
    {
        public OutputPlot() { DefaultProperties = new PlotProperties(); }
        public OutputPlot(string name,PlotProperties defaultProperties, PlotStyle style) { Name = name; DefaultProperties = defaultProperties; Style = style; }

        public string Name { get; internal set; } = string.Empty;
        public List<PlotProperties> Properties { get; } = new List<PlotProperties>();
        public TimeDataSeries Series { get; internal set; } = new TimeDataSeries();

        public PlotStyle Style { get; set; } = PlotStyle.Line;
        internal PlotProperties DefaultProperties { get; set; }
        public int CurrentBarIndex { get; internal set; } = -1;

        public TimeDataPoint GetValueAt(int index)
        {
            if (Series == null || index >= Series.Count || index < 0)
                throw new EvolverException("OutputPlot GetValueAt() index out of range.");

            return Series.GetValueAt(index);
        }

        public double this[int barsAgo]
        {
            get
            {
                int n = CurrentBarIndex - barsAgo;

                if (n > Series.Count || n < 0)
                    throw new EvolverException("OutputPlot get[barsAgo] out of range.");

                return Series[n].Value;
            }
            set
            {
                int n = CurrentBarIndex - barsAgo;

                if (n > Series.Count || n < 0)
                    throw new EvolverException("OutputPlot set[barsAgo] out of range.");

                Series[n].Value = value;
            }
        }
    }

    public class InputIndicator
    {
        public InputIndicator(Indicator indicator, int plotIndex) { Indicator = indicator; PlotIndex = plotIndex; }

        public Indicator Indicator { get; internal set; }
        public int PlotIndex { get; internal set; } = -1;

        public int CurrentBarIndex { get; internal set; } = -1;

        public bool CurrentIsEnd()
        {
            OutputPlot oPlot = Indicator.Outputs[PlotIndex];
            if (oPlot.Series == null) return false;

            return CurrentBarIndex == oPlot.Series.Count - 1;
        }
    }

    public class BarsPointer
    {
        public BarsPointer(InstrumentDataSlice slice) { Slice = slice; }
        public InstrumentDataSlice Slice { get; internal set; }
        public int CurrentBarIndex { get; internal set; } = -1;

        public InstrumentDataSliceRecord Record { get { return Slice.Record; } }
        public int Count { get { return Slice.Count; } }
        public DataLoadState LoadState { get { return Slice.LoadState; } }
        public DateTime MinTime(int lastCount) { return Slice.MinTime(lastCount); }
        public DateTime MaxTime(int lastCount) { return Slice.MaxTime(lastCount); }

        public bool CurrentIsEnd()
        {
            return CurrentBarIndex == Slice.Count - 1;
        }

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
                int n = CurrentBarIndex - barsAgo;
                return Slice.GetValueAt(n);
            }
            set
            {

                int n = CurrentBarIndex - barsAgo;
                Slice.SetValueAt(value,n);
            }
        }
    }

    [Serializable]
    public class IndicatorProperties
    {
        
    }

    public class Indicator
    {
        public Indicator(IndicatorProperties properties) { Properties = properties; }

        internal event EventHandler? DataChanged;
        public string Name { get; set; } = string.Empty;

        public IndicatorProperties Properties { get; internal set; }

        public IndicatorState State { get; internal set; } = IndicatorState.New;



        public int CurrentInputIndex { get; internal set; } = -1;
        public int CurrentBarIndex { get; internal set; } = -1;

        public int CurrentBarsIndex { get; internal set; } = -1;
        public int CurrentInputsIndex { get; internal set; } = -1;

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
            if (_sourceRecord.SourceBarData != null && _sourceRecord.SourceType == CalculationSource.BarData)
            {
                Bars.Add(new BarsPointer(_sourceRecord.SourceBarData));
                if (DataChanged != null) DataChanged(this, EventArgs.Empty);
            }
            if (_sourceRecord.SourceIndicator != null && _sourceRecord.SourceType == CalculationSource.IndicatorPlot)
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
                    return Inputs[0].Indicator.SelectOutputPointsInRange(min, max, Inputs[0].PlotIndex).Select(tuple => tuple.Item1);
            }

            return Enumerable.Empty<IDataPoint>();
        }
        internal IEnumerable<(IDataPoint, int)> SelectOutputPointsInRange(DateTime min, DateTime max, int plotIndex, bool skipLeadingNaN = false)
        {
            if (plotIndex < 0 || plotIndex >= Outputs.Count) return Enumerable.Empty<(IDataPoint, int)>();

            return Outputs[plotIndex].Series
                    .Select((item, index) => (item!, index))
                    .Where(tuple => tuple.Item1.X >= min && tuple.Item1.X <= max)
                    .Select(t => ((IDataPoint)t.Item1, t.Item2));
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

        private void resetCurrentBars()
        {
            CurrentBarIndex = -1;
            CurrentBarsIndex = -1;
            foreach (BarsPointer bars in Bars)
            {
                bars.CurrentBarIndex = -1;
            }
            foreach (InputIndicator input in Inputs)
            {
                input.CurrentBarIndex = -1;
            }
            foreach (OutputPlot output in Outputs)
            {
                output.CurrentBarIndex = -1;
            }
        }

        internal void Startup()
        {
            State = IndicatorState.Startup;
            Configure();
        }

        internal void RunHistory()
        {
            State = IndicatorState.History;
            resetCurrentBars();

            if (IsDataOnly) return;

            while (true)
            {
                bool moreHistory = false;
                foreach (BarsPointer bars in Bars)
                {
                    if (!bars.CurrentIsEnd()) { moreHistory = true; break; }
                }
                if (!moreHistory)
                {
                    foreach (InputIndicator input in Inputs)
                    {
                        if (input.CurrentIsEnd()) { moreHistory = true; break; }
                    }
                }

                if (moreHistory)
                    runHistoryNextData();
                else
                    break;
            }

            if(DataChanged != null) DataChanged(this, EventArgs.Empty);
        }

        private void runHistoryNextData()
        {
            if (_sourceRecord == null)
                throw new EvolverException("Attempting to run a sourceless indicator.");

            //next input:
            int inputIndex = -1;
            int barsIndex = -1;
            DateTime nextDataTime = DateTime.MaxValue;
            for (int i = 0; i < Bars.Count; i++)
            {
                DateTime x = Bars[i].GetValueAt(Bars[i].CurrentBarIndex + 1).Time;
                if ( x < nextDataTime)
                {
                    barsIndex = i;
                    nextDataTime = x;
                }
            }
            for (int i = 0; i < Inputs.Count; i++)
            {
                DateTime x = Inputs[i].Indicator.Outputs[Inputs[i].PlotIndex].GetValueAt(Inputs[i].CurrentBarIndex + 1).Time;
                if (x < nextDataTime)
                {
                    inputIndex = i;
                    barsIndex = -1;
                    nextDataTime = x;
                }
            }

            if (inputIndex >= 0)
            {
                Inputs[inputIndex].CurrentBarIndex++;
                CurrentBarIndex = -1;
                CurrentBarsIndex = -1;
                CurrentInputsIndex = inputIndex;
                CurrentInputIndex = Inputs[inputIndex].CurrentBarIndex;
                if (_sourceRecord.SourceType == CalculationSource.IndicatorPlot && inputIndex == 0)
                {
                    foreach (OutputPlot outputPlot in Outputs)
                    {
                        outputPlot.CurrentBarIndex++;
                        outputPlot.Series.Add(new TimeDataPoint(nextDataTime, double.NaN));
                        outputPlot.Properties.Add(new PlotProperties(outputPlot.DefaultProperties));
                    }
                }
            }
            else if (barsIndex >= 0)
            {
                Bars[barsIndex].CurrentBarIndex++;
                CurrentBarsIndex = barsIndex;
                CurrentBarIndex = Bars[barsIndex].CurrentBarIndex;
                CurrentInputsIndex = -1;
                CurrentInputIndex = -1;

                if (_sourceRecord.SourceType == CalculationSource.BarData && barsIndex == 0)
                {
                    foreach (OutputPlot outputPlot in Outputs)
                    {
                        outputPlot.CurrentBarIndex++;
                        outputPlot.Series.Add(new TimeDataPoint(nextDataTime, double.NaN));
                        outputPlot.Properties.Add(new PlotProperties(outputPlot.DefaultProperties));
                    }
                }
            }

            OnDataUpdate();
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
