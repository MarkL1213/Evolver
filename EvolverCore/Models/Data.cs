using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EvolverCore.Models;
using MessagePack;
using Microsoft.VisualBasic;

namespace EvolverCore
{
    [Serializable]
    public class InstrumentDataSlice
    {
        //only serialize the record. re-resolve the data referenec on de-serialization
        public InstrumentDataSliceRecord Record { get; internal set; } = new InstrumentDataSliceRecord();

        int _startDateOffset = -1;
        int _endDateOffset = -1;
        InstrumentDataSeries? _series = null;

        public int Count
        {
            get
            {
                if (_startDateOffset == -1 || _endDateOffset == -1 || _series == null) return 0;
                return _endDateOffset - _startDateOffset;
            }
        }

        public TimeDataBar GetValueAt(int index)
        {
            throw new EvolverException("TBI");
        }

        //async task to request load of underlying data
        //set min/max index offsets during load

        //hide data reference
        //add element access that offsets by the slice's min/max
    }

    [Serializable]
    public class InstrumentDataSliceRecord
    {
        public InstrumentDataSliceRecord() { }
        public Instrument Instrument {get;internal set;} = new Instrument();

        public DataInterval Interval { get; internal set; } = new DataInterval() { Type = global::EvolverCore.Interval.Minute, Value = 1 };

        public DateTime StartDate { get; internal set; }

        public DateTime EndDate { get; internal set; }
    }


    [Serializable]
    public class IndicatorDataSlice
    {
        //only serialize the record. re-resolve the data reference on de-serialization
        public IndicatorDataSliceRecord Record { get; internal set; } = new IndicatorDataSliceRecord();

        int _startDateOffset = -1;
        int _endDateOffset = -1;
        TimeDataSeries? _plot0 = null;
        List<List<double>> _plots = new List<List<double>>();

        InstrumentDataSlice? _inputSeries = null;
        IndicatorDataSlice? _inputIndicator = null;

        public DataInterval Interval
        {
            get
            {
                if (Record.SourceType == CalculationSource.BarData && _inputSeries != null)
                    return _inputSeries.Record.Interval;
                else if (Record.SourceType == CalculationSource.IndicatorPlot && _inputIndicator != null)
                    return _inputIndicator.Interval;
                return new DataInterval(EvolverCore.Interval.Hour, 1);
            }
        }

        public int InputElementCount
        {
            get
            {
                if (Record.SourceType == CalculationSource.BarData && _inputSeries != null)
                { return _inputSeries.Count; }
                else if (Record.SourceType == CalculationSource.IndicatorPlot && _inputIndicator != null)
                {
                    return _inputIndicator.InputElementCount;
                }
                return 0;
            }
        }

        public int OutputElementCount(int plotIndex)
        {
            if (plotIndex < 0) return 0;
            if (plotIndex == 0) return _plot0 != null ? _plot0.Count : 0;
            if (_plots != null && _plots.Count >= plotIndex) return _plots[plotIndex - 1].Count;
            return 0;
        }

        public IEnumerable<IDataPoint> SelectInputPointsInRange(DateTime min, DateTime max)
        {
            return new List<TimeDataPoint>();
        }
        public IEnumerable<IDataPoint> SelectOutputPointsInRange(DateTime min, DateTime max, int plotIndex, bool skipLeadingNaN = false)
        {
            return new List<TimeDataPoint>();
        }

        public DateTime MinTime(int lastCount)
        {
            DateTime result = DateTime.MaxValue;
            if (Record.SourceType == CalculationSource.BarData && _inputSeries != null)
            {
                int n = 1;
                for (int i = _endDateOffset; i >= _startDateOffset; i--)
                {
                    if (n++ > lastCount) return result;
                    if (_inputSeries.GetValueAt(i).Time < result) result = _inputSeries.GetValueAt(i).Time;
                }
            }
            else if (Record.SourceType == CalculationSource.IndicatorPlot && _inputIndicator != null)
                return _inputIndicator.MinTime(lastCount);
            return DateTime.MinValue;
        }

        public DateTime MaxTime(int lastCount)
        {
            DateTime result = DateTime.MinValue;
            if (Record.SourceType == CalculationSource.BarData && _inputSeries != null)
            {
                int n = 1;
                for (int i = _endDateOffset; i >= _startDateOffset; i--)
                {
                    if (n++ > lastCount) return result;
                    if (_inputSeries.GetValueAt(i).Time > result) result = _inputSeries.GetValueAt(i).Time;
                }
            }
            else if (Record.SourceType == CalculationSource.IndicatorPlot && _inputIndicator != null)
                return _inputIndicator.MinTime(lastCount);
            return DateTime.MaxValue;
        }

        //async task to request load of underlying data
        //set min/max index offsets during load

        //hide data reference
        //add element access that offsets by the slice's min/max

    }

    public enum CalculationSource
    {
        BarData,
        IndicatorPlot
    }

    [Serializable]
    public class IndicatorDataSliceRecord
    {
        public CalculationSource SourceType { get; internal set; } = CalculationSource.BarData;

        public IndicatorDataSliceRecord? SourceIndicator { get; internal set; }
        public int SourcePlotIndex { get; internal set; } = -1;

        public InstrumentDataSliceRecord? SourceBarData { get; internal set; } = null;

        public DateTime StartDate { get; internal set; }

        public DateTime EndDate { get; internal set; }
    }

    public enum Interval
    {
        Second,
        Minute,
        Hour,
        Day,
        Week,
        Month,
        Year
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

    public struct DataInterval
    {
        public Interval Type;
        public int Value;

        public DataInterval(Interval type, int value) { Type = type; Value = value; }

        public DateTime Add(DateTime dateTime,int n)
        {
            switch (Type)
            {
                case Interval.Second: return dateTime.AddSeconds(Value * n);
                case Interval.Minute: return dateTime.AddMinutes(Value * n);
                case Interval.Hour: return dateTime.AddHours(Value * n);
                case Interval.Day: return dateTime.AddDays(Value * n);
                case Interval.Week: return dateTime.AddDays(Value * n * 7);
                case Interval.Month: return dateTime.AddMonths(Value * n);
                case Interval.Year: return dateTime.AddYears(Value * n);
                default:
                    throw new EvolverException($"Unknown interval type in interval.Add() : type={Type}");
            }
        }

        public long Ticks
        {
            get
            {
                DateTime now = DateTime.Now;
                DateTime then = Add(now,1);
                return (now - then).Ticks;
            }
        }
    }

    public interface IDataPoint
    {
        public DateTime X { get; }
        public double Y { get; }
    }

    public record TimeDataBar : IDataPoint
    {
        public TimeDataBar(DateTime time, double open, double high, double low, double close, long volume, double bid, double ask)
        {
            Time = DateTime.SpecifyKind(time, time.Kind);
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            Bid = bid;
            Ask = ask;
        }

        public DateTime Time;
        public DateTime X { get { return Time; } }
        public double Y
        {
            get
            {
                return Close;
            }
        }

        public double Open;
        public double High;
        public double Low;
        public double Close;
        public long Volume;
        public double Bid;
        public double Ask;

        public static TimeDataBar Empty
        {
            get
            {
                return new TimeDataBar(DateTime.MinValue, 0, 0, 0, 0, 0, 0, 0);
            }
        }

        public override string ToString()
        {
            return $"BDP [T:{Time} O:{Open} H:{High} L:{Low} C:{Close} B:{Bid} A:{Ask} V:{Volume}]";
        }
    }

    public record TimeDataPoint : IDataPoint
    {
        public DateTime Time;
        public double Value;

        public TimeDataPoint(DateTime time, double value) { Time = time; Value = value; }

        public DateTime X { get { return Time; } }
        public double Y { get { return Value; } }

        public static TimeDataPoint Empty
        {
            get
            {
                return new TimeDataPoint(DateTime.MinValue, double.NaN);
            }
        }

        public override string ToString()
        {
            return $"TDP [X:{X} Y:{Y}]";
        }
    }

    public interface IDataSeries<T> where T : IDataPoint
    {
        public IEnumerable<T> Where(Func<T, bool> predicate);
        public IEnumerable<T> Select(Func<T, int, T> selector);

        public int Count { get; }

        public T GetValue(int barsAgo);
    }

    public class DataSeries<T> : IDataSeries<T> where T : IDataPoint
    {
        List<T> _values = new List<T>();

        public DataSeries() { }

        public void Clear() { _values.Clear(); }

        public int Count { get { return _values.Count; } }

        public DataInterval Interval { get; internal set; }

        public IEnumerator<T> GetEnumerator() { return _values.GetEnumerator(); }

        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            return _values.Where(predicate);
        }

        public IEnumerable<T> Select(Func<T, int, T> selector)
        {
            return _values.Select(selector);
        }

        public DateTime Min(Func<T, DateTime> selector)
        {
            return _values.Min(selector);
        }
        public double Min(Func<T, double> selector)
        {
            return _values.Min(selector);
        }

        public DateTime Max(Func<T, DateTime> selector)
        {
            return _values.Max(selector);
        }
        public double Max(Func<T, double> selector)
        {
            return _values.Max(selector);
        }

        public bool IsDataValid(int barsAgo)
        {
            int c = _values.Count - 1;
            return !(barsAgo < 0 || barsAgo >= c);

        }
        public bool IsDataValidAt(int index)
        {
            return !(index < 0 || index >= _values.Count);
        }

        public IEnumerable<T> TakeLast(int count)
        {
            return _values.TakeLast(count);
        }

        public IEnumerable<T> Tolist()
        {
            return _values.ToList();
        }

        public IEnumerable<T> SkipWhile(Func<T, bool> skipper)
        {
            return _values.SkipWhile(skipper);
        }

        public T GetValueAt(int index) { return _values[index]; }
        public T GetValue(int barsAgo) { return this[barsAgo]; }
        public T this[int barsAgo]
        {
            get
            {
                int c = _values.Count - 1;
                if (barsAgo < 0 || barsAgo >= c)
                {
                    throw new EvolverException();
                }
                return _values[c - barsAgo];
            }
            internal set { }
        }

        public virtual void Add(T value) { _values.Add(value); }

        internal void Save(string fileName)
        {
            try
            {
                using (FileStream fs = File.Open(fileName, File.Exists(fileName) ? FileMode.Truncate : FileMode.CreateNew, FileAccess.Write))
                {
                    BinaryWriter bw = new BinaryWriter(fs);
                    foreach (T value in _values)
                    {
                        byte[] bytes = MessagePackSerializer.Serialize<T>(value);
                        bw.Write(bytes.Length);
                        bw.Write(bytes);
                    }

                }
            }
            catch (Exception e)
            {
                throw new EvolverException("Unable to save data.", e);
            }
        }
        internal static DataSeries<T> Load(string fileName)
        {
            DataSeries<T> series = new DataSeries<T>();

            try
            {
                using (FileStream fs = File.Open(fileName, File.Exists(fileName) ? FileMode.Truncate : FileMode.CreateNew, FileAccess.Write))
                {
                    BinaryReader br = new BinaryReader(fs);
                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        int size = br.ReadInt32();

                        byte[] bytes = br.ReadBytes(size);
                        T value = MessagePackSerializer.Deserialize<T>(bytes);

                        series._values.Add(value);
                    }
                }
            }
            catch (Exception e)
            {
                throw new EvolverException("Unable to load data.", e);
            }

            return series;
        }

        internal static long IntervalTicks(DataSeries<IDataPoint> series)
        {
            if (series.Count < 2) return TimeSpan.FromMinutes(1).Ticks;
            return (series[1].X - series[0].X).Ticks;
        }
    }

    public class TimeDataSeries : DataSeries<TimeDataPoint>
    {
        TimeZoneInfo TimeZoneInfo { get; set; }
        public TimeDataSeries()
        {
            TimeZoneInfo = TimeZoneInfo.Local;
        }

        internal static long IntervalTicks(TimeDataSeries series)
        {
            if (series.Count < 2) return TimeSpan.FromMinutes(1).Ticks;
            return (series[1].X - series[0].X).Ticks;
        }

    }

    public record BarPricePoint : IDataPoint
    {
        private readonly TimeDataBar _bar;
        private readonly BarPriceValue _field;

        public BarPricePoint(TimeDataBar? bar, BarPriceValue field)
        {
            _bar = bar ?? throw new ArgumentNullException(nameof(bar));
            _field = field;
        }

        public DateTime X => _bar.Time;

        public double Y => _field switch
        {
            BarPriceValue.Open => _bar.Open,
            BarPriceValue.High => _bar.High,
            BarPriceValue.Low => _bar.Low,
            BarPriceValue.Close => _bar.Close,
            BarPriceValue.Bid => _bar.Bid,
            BarPriceValue.Ask => _bar.Ask,
            BarPriceValue.Volume => _bar.Volume,
            BarPriceValue.HL => (_bar.High + _bar.Low) / 2,
            BarPriceValue.OC => (_bar.Open + _bar.Close) / 2,
            BarPriceValue.HLC => (_bar.High + _bar.Low + _bar.Close) / 3,
            BarPriceValue.OHLC => (_bar.Open + _bar.High + _bar.Low + _bar.Close) / 4,
            _ => throw new NotSupportedException($"Unsupported PriceField: {_field}")
        };

        public override string ToString()
        {
            return $"BPP {_field} [X:{X} Y:{Y}]";
        }
    }

    public class BarDataSeries : DataSeries<TimeDataBar>
    {
        TimeZoneInfo TimeZoneInfo { get; set; }

        public BarDataSeries()
        {
            TimeZoneInfo = TimeZoneInfo.Local;
        }

        internal BarPriceValue ValueType { set; get; } = BarPriceValue.Close;

        internal static long IntervalTicks(BarDataSeries series)
        {
            if (series.Count < 2) return TimeSpan.FromMinutes(1).Ticks;
            return (series[1].X - series[0].X).Ticks;
        }

        public static BarDataSeries? RandomSeries(DateTime startTime, DataInterval interval,int size)
        {
            BarDataSeries barDataSeries = new BarDataSeries();
            barDataSeries.Interval = interval;
            Random r = new Random(DateTime.Now.Second);

            int lastClose = -1;
            for (int i = 0; i < size; i++)
            {
                int open = lastClose == -1 ? r.Next(10, 100) : lastClose;
                int close = r.Next(10, 100);
                int volume = r.Next(100, 1000);
                int high = open > close ? open + r.Next(0, 15) : close + r.Next(0, 15);
                int low = open > close ? close - r.Next(0, 15) : open - r.Next(0, 15);

                TimeDataBar bar = new TimeDataBar(startTime, open, high, low, close, volume, 0, 0);
                barDataSeries.Add(bar);
                lastClose = close;
                startTime = interval.Add(startTime, 1);
            }

            return barDataSeries;
        }
    }

    public class InstrumentDataSeries : BarDataSeries
    {
        public Instrument? Instrument { get; internal set; }

    }


    internal class DataManager
    {
        //handle all data load/save/infoscan/organization/update operations
        InstrumentDataRecordCollection? _instrumentDataCollection = new InstrumentDataRecordCollection();
        public InstrumentDataRecordCollection? InstrumentDataCollection { get { return _instrumentDataCollection; } }

        public List<Indicator> _indicatorCache = new List<Indicator>();

        public delegate void DataChangeDelegate(InstrumentDataRecordCollection instrumentRecord);
        public event DataChangeDelegate? DataChange = null;


    }
}