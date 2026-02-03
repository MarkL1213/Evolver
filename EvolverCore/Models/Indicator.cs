using Avalonia.Media;
using Avalonia.Media.Immutable;
using EvolverCore.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

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

    public enum CalculationSource
    {
        BarData,
        IndicatorPlot
    }

    public enum BarPriceValue
    {
        Open,
        High,
        Low,
        Close,
        Volume,
        Bid,
        Ask,
        OC,
        HL,
        HLC,
        OHLC
    }

    [Serializable]
    public class IndicatorDataSourceRecord
    {
        public CalculationSource SourceType { get; internal set; } = CalculationSource.BarData;

        public Indicator? SourceIndicator { get; internal set; }
        public int SourcePlotIndex { get; internal set; } = -1;

        public BarTablePointer? SourceBarData { get; internal set; } = null;

        public DateTime StartDate { get; internal set; }

        public DateTime EndDate { get; internal set; }
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

        public Pen CreateLinePen()
        {
            return new Pen(PlotLineBrush, PlotLineThickness, PlotLineStyle);
        }

        public bool ValueEquals(PlotProperties b)
        {
            if (b == null) return false;

            if (PlotLineThickness != b.PlotLineThickness) return false;

            if (PlotLineStyle == null && b.PlotLineStyle != null) return false;
            if (PlotLineStyle != null && b.PlotLineStyle == null) return false;
            if (PlotLineStyle != null && b.PlotLineStyle != null)
            {
                if (PlotLineStyle.Offset != b.PlotLineStyle.Offset) return false;

                if (PlotLineStyle.Dashes == null && b.PlotLineStyle.Dashes != null) return false;
                if (PlotLineStyle.Dashes != null && b.PlotLineStyle.Dashes == null) return false;
                if (PlotLineStyle.Dashes != null && b.PlotLineStyle.Dashes != null)
                {
                    if (PlotLineStyle.Dashes.Count != b.PlotLineStyle.Dashes.Count) return false;
                    for (int i = 0; i < PlotLineStyle.Dashes.Count; i++)
                    {
                        if (PlotLineStyle.Dashes[i] != b.PlotLineStyle.Dashes[i]) return false;
                    }
                }
            }

            if (PlotLineBrush == null && b.PlotLineBrush != null) return false;
            if (PlotLineBrush != null && b.PlotLineBrush == null) return false;
            if (PlotLineBrush != null && b.PlotLineBrush != null)
            {
                if (PlotLineBrush.GetType() != b.PlotLineBrush.GetType()) return false;

                if (PlotLineBrush is SolidColorBrush)
                {
                    SolidColorBrush brushA = (PlotLineBrush as SolidColorBrush)!;
                    SolidColorBrush brushB = (b.PlotLineBrush as SolidColorBrush)!;

                    if (brushA.Color != brushB.Color) return false;
                    if (brushA.Opacity != brushB.Opacity) return false;
                }
                else if (PlotLineBrush is ImmutableSolidColorBrush)
                {
                    ImmutableSolidColorBrush brushA = (PlotLineBrush as ImmutableSolidColorBrush)!;
                    ImmutableSolidColorBrush brushB = (b.PlotLineBrush as ImmutableSolidColorBrush)!;

                    if (brushA.Color != brushB.Color) return false;
                    if (brushA.Opacity != brushB.Opacity) return false;
                }
                else
                {
                    throw new EvolverException($"Brush type {PlotLineBrush.GetType()} value comparison not implemented.");
                }
            }

            if (PlotFillBrush == null && b.PlotFillBrush != null) return false;
            if (PlotFillBrush != null && b.PlotFillBrush == null) return false;
            if (PlotFillBrush != null && b.PlotFillBrush != null)
            {
                if (PlotFillBrush.GetType() != b.PlotFillBrush.GetType()) return false;

                if (PlotFillBrush is SolidColorBrush)
                {
                    SolidColorBrush brushA = (PlotFillBrush as SolidColorBrush)!;
                    SolidColorBrush brushB = (b.PlotFillBrush as SolidColorBrush)!;

                    if (brushA.Color != brushB.Color) return false;
                    if (brushA.Opacity != brushB.Opacity) return false;
                }
                else if (PlotFillBrush is ImmutableSolidColorBrush)
                {
                    ImmutableSolidColorBrush brushA = (PlotFillBrush as ImmutableSolidColorBrush)!;
                    ImmutableSolidColorBrush brushB = (b.PlotFillBrush as ImmutableSolidColorBrush)!;

                    if (brushA.Color != brushB.Color) return false;
                    if (brushA.Opacity != brushB.Opacity) return false;
                }
                else
                {
                    throw new EvolverException($"Brush type {PlotFillBrush.GetType()} value comparison not implemented.");
                }
            }
            return true;
        }
    }

    public class PlotPropertyCollection
    {
        OutputPlot _parent;
        private List<PlotProperties> _properties = new List<PlotProperties>();
        public PlotPropertyCollection(OutputPlot parent) { _parent = parent; }

        public PlotProperties GetValueAt(int index)
        {
            return _properties[index];
        }

        public List<PlotProperties> ToList() { return _properties.ToList(); }

        public List<PlotProperties> GetRange(int index, int count) { return _properties.GetRange(index, count); }

        public void Add(PlotProperties prop) { _properties.Add(prop); }

        public PlotProperties this[int barsAgo]
        {
            get
            {
                int n = _parent.CurrentBarIndex - barsAgo;

                if (n > _properties.Count || n < 0)
                    throw new EvolverException("PlotPropertyCollection get[barsAgo] out of range.");

                return _properties[n];
            }
            set
            {
                int n = _parent.CurrentBarIndex - barsAgo;

                if (n > _properties.Count || n < 0)
                    throw new EvolverException("PlotPropertyCollection set[barsAgo] out of range.");

                _properties[n] = value;
            }
        }

    }

    public class OutputPlot
    {
        public OutputPlot()
        {
            DefaultProperties = new PlotProperties();
            Properties = new PlotPropertyCollection(this);
            //Series = new DataTableColumn<double>("", DataType.Double, 0);
        }
        public OutputPlot(string name, PlotProperties defaultProperties, PlotStyle style)
        {
            Name = name;
            DefaultProperties = defaultProperties;
            Style = style;
            Properties = new PlotPropertyCollection(this);
            //Series = new DataTableColumn<double>(name, DataType.Double, 0);
        }

        public string Name { get; internal set; } = string.Empty;
        public PlotPropertyCollection Properties { get; }
        //public DataTableColumn<double> Series { get; internal set; } 

        public PlotStyle Style { get; set; } = PlotStyle.Line;
        internal PlotProperties DefaultProperties { get; set; }
        public int CurrentBarIndex { get; internal set; } = -1;

        //public double GetValueAt(int index)
        //{
        //    if (Series == null || index >= Series.Count || index < 0)
        //        throw new EvolverException("OutputPlot GetValueAt() index out of range.");

        //    return Series.GetValueAt(index);
        //}

        //public double this[int barsAgo]
        //{
        //    get
        //    {
        //        int n = CurrentBarIndex - barsAgo;

        //        if (n > Series.Count || n < 0)
        //            throw new EvolverException("OutputPlot get[barsAgo] out of range.");

        //        return Series.GetValueAt(n);
        //    }
        //    set
        //    {
        //        int n = CurrentBarIndex - barsAgo;

        //        if (n > Series.Count || n < 0)
        //            throw new EvolverException("OutputPlot set[barsAgo] out of range.");

        //        Series.SetValueAt(value, n);
        //    }
        //}
    }

    public class InputIndicator
    {
        public InputIndicator(Indicator indicator, int plotIndex) { Indicator = indicator; PlotIndex = plotIndex; }

        public Indicator Indicator { get; internal set; }
        public int PlotIndex { get; internal set; } = -1;

        public int CurrentBarIndex { get; internal set; } = -1;

        public bool CurrentIsEnd()
        {
            DataTableColumn<double>? series = Indicator.Outputs.Column(Indicator.Plots[PlotIndex].Name) as DataTableColumn<double>;
            if (series == null) return false;

            return CurrentBarIndex == series.Count - 1;
        }
        public double GetValueAt(int index)
        {
            DataTableColumn<double>? series = Indicator.Outputs.Column(Indicator.Plots[PlotIndex].Name) as DataTableColumn<double>;
            if (series == null)
                throw new EvolverException("InputIndicator output series is not a double.");

            if (index >= series.Count || index < 0)
                    throw new EvolverException("InputIndicator GetValueAt() index out of range.");

            return series.GetValueAt(index);
        }

        public double this[int barsAgo]
        {
            get
            {
                DataTableColumn<double>? series = Indicator.Outputs.Column(Indicator.Plots[PlotIndex].Name) as DataTableColumn<double>;
                if (series == null)
                    throw new EvolverException("InputIndicator output series is not a double.");

                int n = CurrentBarIndex - barsAgo;

                if (n > series.Count || n < 0)
                    throw new EvolverException("InputIndicator get[barsAgo] out of range.");

                return series.GetValueAt(n);
            }
            set
            {
                DataTableColumn<double>? series = Indicator.Outputs.Column(Indicator.Plots[PlotIndex].Name) as DataTableColumn<double>;
                if (series == null)
                    throw new EvolverException("InputIndicator output series is not a double.");

                int n = CurrentBarIndex - barsAgo;

                if (n > series.Count || n < 0)
                    throw new EvolverException("InputIndicator set[barsAgo] out of range.");

                series.SetValueAt(value,n);
            }
        }
    }

    //public class BarsPointer
    //{
    //    public BarsPointer(InstrumentDataSlice slice) { Slice = slice; }
    //    public InstrumentDataSlice Slice { get; internal set; }
    //    public int CurrentBarIndex { get; internal set; } = -1;

    //    public InstrumentDataSliceRecord Record { get { return Slice.Record; } }
    //    public int Count { get { return Slice.Count; } }
    //    public DataLoadState LoadState { get { return Slice.LoadState; } }
    //    public DateTime MinTime(int lastCount) { return Slice.MinTime(lastCount); }
    //    public DateTime MaxTime(int lastCount) { return Slice.MaxTime(lastCount); }

    //    public bool CurrentIsEnd()
    //    {
    //        return CurrentBarIndex == Slice.Count - 1;
    //    }

    //    public IEnumerable<TimeDataBar> Where(Func<TimeDataBar, bool> predicate)
    //    {
    //        return Slice.Where(predicate);
    //    }

    //    public TimeDataBar GetValueAt(int i)
    //    {
    //        return Slice.GetValueAt(i);
    //    }

    //    public TimeDataBar this[int barsAgo]
    //    {
    //        get
    //        {
    //            int n = CurrentBarIndex - barsAgo;
    //            return Slice.GetValueAt(n);
    //        }
    //        set
    //        {

    //            int n = CurrentBarIndex - barsAgo;
    //            Slice.SetValueAt(value, n);
    //        }
    //    }
    //}

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
        public List<BarTablePointer> Bars { get; internal set; } = new List<BarTablePointer>();
        public List<InputIndicator> Inputs { get; internal set; } = new List<InputIndicator>();
        public DataTableBase Outputs { get; internal set; } = new DataTableBase();
        public List<OutputPlot> Plots { get; internal set; } = new List<OutputPlot>();


        //////////////////
        internal void SetSourceData(IndicatorDataSourceRecord sourceRecord)
        {
            _sourceRecord = sourceRecord;
            if (_sourceRecord.SourceBarData != null && _sourceRecord.SourceType == CalculationSource.BarData)
            {
                Bars.Add(_sourceRecord.SourceBarData);
                if (DataChanged != null) DataChanged(this, EventArgs.Empty);
            }
            if (_sourceRecord.SourceIndicator != null && _sourceRecord.SourceType == CalculationSource.IndicatorPlot)
            {
                Inputs.Add(new InputIndicator(_sourceRecord.SourceIndicator, _sourceRecord.SourcePlotIndex));
                if (DataChanged != null) DataChanged(this, EventArgs.Empty);
            }
        }

        internal void OnInputDataLoaded(object? sender, EventArgs e)
        {
            //FIXME -- an input added during configure() has been loaded, set it up

            if (!WaitingForDataLoad)
            {
                //Globals.Instance.DataManager.IndicatorReadyToRun(this);
            }
        }

        internal void OnSourceDataLoaded(object? sender, EventArgs e)
        {
            if (_sourceRecord == null) { return; }

            //InstrumentDataSlice? sourceInstrumentSlice = sender as InstrumentDataSlice;
            //if (sourceInstrumentSlice != null) sourceInstrumentSlice.DataLoaded -= OnSourceDataLoaded;

            //if (_sourceRecord.SourceBarData != null)
            //{
            //    Bars.Add(_sourceRecord.SourceBarData);
            //    if (DataChanged != null) DataChanged(this, EventArgs.Empty);
            //}

            if (!WaitingForDataLoad)
            {
                //Globals.Instance.DataManager.IndicatorReadyToRun(this);
            }
        }

        public void AddOutput(OutputPlot output)// new OutputPlot("EMA", plotProperties, PlotStyle.Line))
        {
            Outputs.AddColumn(output.Name);
            Plots.Add(output);
        }


        internal BarTablePointer SliceSourcePointsInRange(DateTime min, DateTime max)
        {
            if (_sourceRecord != null)
            {
                return Bars[0].Slice(min, max);
            }

            throw new EvolverException("No source defined.");
        }

        internal (int min, int max) IndexOfSourcePointsInRange(DateTime min, DateTime max)
        {
            int minIndex = Bars[0].Time.FindIndex(min);
            int maxIndex = Bars[0].Time.FindIndex(max);

            if (minIndex == -1 || maxIndex == -1)
                throw new IndexOutOfRangeException("Unable to slice min/max out of range.");

            return (minIndex, maxIndex);
        }


        object _outputLock = new object();

        internal ColumnPointer<double> SliceOutputPointsInRange(DateTime min, DateTime max, int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= Plots.Count) throw new EvolverException("Invalid plot index.");

            lock (_outputLock)
            {
                int minIndex = Bars[0].Time.FindIndex(min);
                int maxIndex = Bars[0].Time.FindIndex(max);

                if (minIndex == -1 || maxIndex == -1)
                    throw new IndexOutOfRangeException("Unable to slice min/max out of range.");

                DataTableColumn<double>? column = Outputs.Columns[plotIndex] as DataTableColumn<double>;
                if (column == null)
                    throw new EvolverException("");

                return  column.Slice(minIndex, maxIndex);
            }
        }

        internal (int min, int max) IndexOfOutputPointsInRange(DateTime min, DateTime max, int plotIndex)
        {
            if (plotIndex < 0 || plotIndex >= Plots.Count) throw new EvolverException("Invalid plot index.");

            lock (_outputLock)
            {
                int minIndex = Bars[0].Time.FindIndex(min);
                int maxIndex = Bars[0].Time.FindIndex(max);

                if (minIndex == -1 || maxIndex == -1)
                    throw new IndexOutOfRangeException("Unable to slice min/max out of range.");

                return (minIndex, maxIndex);
            }
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
                    return Bars[0].RowCount;
                else if (_sourceRecord.SourceType == CalculationSource.IndicatorPlot && Inputs.Count > 0)
                    return Inputs[0].Indicator.OutputElementCount(Inputs[0].PlotIndex);
            }
            return 0;
        }

        public int OutputElementCount(int plotIndex)
        {
            lock (_outputLock)
            {
                if (plotIndex < 0 || plotIndex >= Plots.Count) return 0;

                return Outputs.Columns[plotIndex].Count;
            }
        }

        public DataInterval Interval
        {
            get
            {
                if (_sourceRecord != null)
                {
                    if (_sourceRecord.SourceType == CalculationSource.BarData && Bars.Count > 0)
                        return Bars[0].Interval;
                    else if (_sourceRecord.SourceType == CalculationSource.IndicatorPlot && Inputs.Count > 0)
                        return Inputs[0].Indicator.Interval;
                }
                return new DataInterval(IntervalSpan.Hour, 1);
            }
        }

        public bool WaitingForDataLoad
        {
            get
            {
                foreach (BarTablePointer bars in Bars)
                {
                    if (bars.State != TableLoadState.Loaded) return true;
                }
                foreach (InputIndicator input in Inputs)
                {
                    if (input.Indicator.WaitingForDataLoad) return true;
                }

                return false;
            }
        }

        private void resetCurrentBars()
        {
            CurrentBarIndex = -1;
            CurrentBarsIndex = -1;
            foreach (BarTablePointer bars in Bars)
            {
                bars.ResetCurrentBar();
            }
            foreach (InputIndicator input in Inputs)
            {
                input.CurrentBarIndex = -1;
            }
            //lock (_outputLock)
            //{
            //    foreach (OutputPlot output in Outputs)
            //    {
            //        output.CurrentBarIndex = -1;
            //    }
            //}
        }

        internal void Startup()
        {
            State = IndicatorState.Startup;
            Configure();
        }

        //internal void RunHistory()
        //{
        //    State = IndicatorState.History;
        //    resetCurrentBars();

        //    if (IsDataOnly) return;

        //    while (true)
        //    {
        //        bool moreHistory = false;
        //        foreach (BarTablePointer bars in Bars)
        //        {
        //            if (!bars.CurrentIsEnd()) { moreHistory = true; break; }
        //        }
        //        if (!moreHistory)
        //        {
        //            foreach (InputIndicator input in Inputs)
        //            {
        //                if (!input.CurrentIsEnd()) { moreHistory = true; break; }
        //            }
        //        }

        //        if (moreHistory)
        //        {
        //            lock (_outputLock)
        //            {
        //                runHistoryNextData();
        //            }
        //        }
        //        else
        //            break;
        //    }

        //    if (DataChanged != null) DataChanged(this, EventArgs.Empty);
        //}

        //private void runHistoryNextData()
        //{
        //    if (_sourceRecord == null)
        //        throw new EvolverException("Attempting to run a sourceless indicator.");

        //    //next input:
        //    int inputIndex = -1;
        //    int barsIndex = -1;
        //    DateTime nextDataTime = DateTime.MaxValue;
        //    for (int i = 0; i < Bars.Count; i++)
        //    {
        //        DateTime x = Bars[i].Time.GetValueAt(Bars[i].CurrentBar + 1);
        //        if (x < nextDataTime)
        //        {
        //            barsIndex = i;
        //            nextDataTime = x;
        //        }
        //    }
        //    for (int i = 0; i < Inputs.Count; i++)
        //    {
        //        DateTime x = Inputs[i].Indicator.Outputs[Inputs[i].PlotIndex].GetValueAt(Inputs[i].CurrentBarIndex + 1).Time;
        //        if (x < nextDataTime)
        //        {
        //            inputIndex = i;
        //            barsIndex = -1;
        //            nextDataTime = x;
        //        }
        //    }

        //    if (inputIndex >= 0)
        //    {
        //        Inputs[inputIndex].CurrentBarIndex++;
        //        CurrentBarIndex = -1;
        //        CurrentBarsIndex = -1;
        //        CurrentInputsIndex = inputIndex;
        //        CurrentInputIndex = Inputs[inputIndex].CurrentBarIndex;
        //        if (_sourceRecord.SourceType == CalculationSource.IndicatorPlot && inputIndex == 0)
        //        {
        //            foreach (OutputPlot outputPlot in Outputs)
        //            {
        //                outputPlot.CurrentBarIndex++;
        //                outputPlot.Series.Add(new TimeDataPoint(nextDataTime, double.NaN));
        //                outputPlot.Properties.Add(new PlotProperties(outputPlot.DefaultProperties));
        //            }
        //        }
        //    }
        //    else if (barsIndex >= 0)
        //    {
        //        Bars[barsIndex].IncrementCurrentBar();
        //        CurrentBarsIndex = barsIndex;
        //        CurrentBarIndex = Bars[barsIndex].CurrentBar;
        //        CurrentInputsIndex = -1;
        //        CurrentInputIndex = -1;

        //        if (_sourceRecord.SourceType == CalculationSource.BarData && barsIndex == 0)
        //        {
        //            foreach (OutputPlot outputPlot in Outputs)
        //            {
        //                outputPlot.CurrentBarIndex++;
        //                outputPlot.Series.Add(new TimeDataPoint(nextDataTime, double.NaN));
        //                outputPlot.Properties.Add(new PlotProperties(outputPlot.DefaultProperties));
        //            }
        //        }
        //    }

        //    OnDataUpdate();
        //}


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
