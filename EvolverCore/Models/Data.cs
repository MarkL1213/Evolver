using EvolverCore.Models;
using MessagePack;
using NP.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace EvolverCore
{
    public enum DataLoadState { NotLoaded, Loading, Loaded, Error };

    [Serializable]
    public class InstrumentDataSlice : IEnumerable<TimeDataBar>
    {
        //only serialize the record. re-resolve the data referenec on de-serialization
        public InstrumentDataSliceRecord Record { get; internal set; } = new InstrumentDataSliceRecord();

        int _startDateOffset = -1;
        int _endDateOffset = -1;
        InstrumentDataSeries? _series = null;
        DataLoadState _loadState = DataLoadState.NotLoaded;

        internal int StartOffset { get { return _startDateOffset; } }
        internal int EndOffset { get { return _endDateOffset; } }

        public DataLoadState LoadState { get { return _loadState; } internal set { _loadState = value; } }

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
            if (_series == null || index < 0 || _startDateOffset + index > _endDateOffset) throw new EvolverException("GetValueAt() index out of range.");
            return _series[_startDateOffset + index];
        }

        public void SetValueAt(TimeDataBar bar, int index)
        {
            if (_series == null || index < 0 || _startDateOffset + index > _endDateOffset) throw new EvolverException("GetValueAt() index out of range.");
            _series[_startDateOffset + index] = bar;
        }
        internal void SetData(InstrumentDataRecord dataRecord)
        {
            if (dataRecord.Data == null)
            {
                _loadState = _loadState == DataLoadState.Error ? DataLoadState.Error : DataLoadState.NotLoaded;
                _series = null;
                _startDateOffset = -1;
                _endDateOffset = -1;
                return;
            }

            //calculate offsets...
            int startOffset = dataRecord.Data.GetNearestIndexOfTime(Record.StartDate);

            int endOffset = dataRecord.Data.GetNearestIndexOfTime(Record.EndDate, startOffset);

            if (startOffset == -1 || endOffset == -1)
            {
                _loadState = DataLoadState.Error;
                _series = null;
                _startDateOffset = -1;
                _endDateOffset = -1;
                return;
            }

            _series = dataRecord.Data;
            _startDateOffset = startOffset;
            _endDateOffset = endOffset;
            _loadState = DataLoadState.Loaded;
        }


        public event EventHandler? DataLoaded;

        public void OnRawDataLoaded(object? sender, InstrumentDataLoadedEventArgs args)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            InstrumentDataRecord? dataRecord = sender as InstrumentDataRecord;
            if (dataRecord == null)
                throw new ArgumentNullException(nameof(sender));

            dataRecord.DataLoaded -= OnRawDataLoaded;

            if (args.Exception != null)
            {
                Globals.Instance.Log.LogException(args.Exception);
                _loadState = DataLoadState.Error;
            }
            else
            {
                //FIXME : BE CAREFULL!! This is likely called from a worker thread.
                SetData(dataRecord);
            }

            DataLoaded?.Invoke(this, new EventArgs());
        }

        public DateTime MinTime(int lastCount)
        {
            if (_series == null) return DateTime.MinValue;

            DateTime result = DateTime.MaxValue;
            int n = 1;
            for (int i = _endDateOffset; i >= _startDateOffset; i--)
            {
                if (n++ > lastCount) return result;
                if (_series.GetValueAt(i).Time < result) result = _series.GetValueAt(i).Time;
            }

            return result;
        }
        public DateTime MaxTime(int lastCount)
        {
            if (_series == null) return DateTime.MaxValue;

            DateTime result = DateTime.MinValue;
            int n = 1;
            for (int i = _endDateOffset; i >= _startDateOffset; i--)
            {
                if (n++ > lastCount) return result;
                if (_series.GetValueAt(i).Time > result) result = _series.GetValueAt(i).Time;
            }

            return result;
        }

        public IEnumerable<TimeDataBar> Where(Func<TimeDataBar, bool> predicate)
        {
            if (_series == null) return Enumerable.Empty<TimeDataBar>();
            return _series.GetRange(_startDateOffset, _endDateOffset - _startDateOffset).Where(predicate);
        }

        IEnumerator<TimeDataBar> IEnumerable<TimeDataBar>.GetEnumerator()
        {
            if (_series == null) return Enumerable.Empty<TimeDataBar>().GetEnumerator();
            return _series.GetRange(_startDateOffset, _endDateOffset - _startDateOffset).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_series == null) return Enumerable.Empty<TimeDataBar>().GetEnumerator();
            return _series.GetRange(_startDateOffset, _endDateOffset - _startDateOffset).GetEnumerator();

        }

        //public TimeDataBar this[int index]
        //{
        //    get
        //    {
        //        return GetValueAt(index);
        //    }
        //    set
        //    {

        //       SetValueAt(value, index);
        //    }
        //}

        //async task to request load of underlying data
        //set min/max index offsets during load

        //hide data reference
        //add element access that offsets by the slice's min/max
    }

    [Serializable]
    public class InstrumentDataSliceRecord
    {
        public InstrumentDataSliceRecord() { }
        public Instrument Instrument { get; internal set; } = new Instrument();

        public DataInterval Interval { get; internal set; } = new DataInterval() { Type = global::EvolverCore.Interval.Minute, Value = 1 };

        public DateTime StartDate { get; internal set; }

        public DateTime EndDate { get; internal set; }
    }


    //[Serializable]
    //public class IndicatorDataSlice
    //{
    //    //only serialize the record. re-resolve the data reference on de-serialization
    //    public IndicatorDataSliceRecord Record { get; internal set; } = new IndicatorDataSliceRecord();

    //    int _startDateOffset = -1;
    //    int _endDateOffset = -1;
    //    TimeDataSeries? _plot0 = null;
    //    List<List<double>> _plots = new List<List<double>>();

    //    InstrumentDataSlice? _inputSeries = null;
    //    IndicatorDataSlice? _inputIndicator = null;

    //    public event EventHandler? DataLoaded;

    //    public void OnSourceDataLoaded(object? source, EventArgs args)
    //    {
    //        if (source is InstrumentDataSlice)
    //        {
    //            InstrumentDataSlice? sourceSlice = source as InstrumentDataSlice;
    //            if(sourceSlice!=null)
    //                sourceSlice.DataLoaded -= OnSourceDataLoaded;
    //        }

    //        if(DataLoaded != null) DataLoaded(this, EventArgs.Empty);
    //    }

    //    public DataInterval Interval
    //    {
    //        get
    //        {
    //            if (Record.SourceType == CalculationSource.BarData && _inputSeries != null)
    //                return _inputSeries.Record.Interval;
    //            else if (Record.SourceType == CalculationSource.IndicatorPlot && _inputIndicator != null)
    //                return _inputIndicator.Interval;
    //            return new DataInterval(EvolverCore.Interval.Hour, 1);
    //        }
    //    }

    //    public int InputElementCount
    //    {
    //        get
    //        {
    //            if (Record.SourceType == CalculationSource.BarData && _inputSeries != null)
    //            { return _inputSeries.Count; }
    //            else if (Record.SourceType == CalculationSource.IndicatorPlot && _inputIndicator != null)
    //            {
    //                return _inputIndicator.InputElementCount;
    //            }
    //            return 0;
    //        }
    //    }



    //    //async task to request load of underlying data
    //    //set min/max index offsets during load

    //    //hide data reference
    //    //add element access that offsets by the slice's min/max

    //}

    public enum CalculationSource
    {
        BarData,
        IndicatorPlot
    }

    [Serializable]
    public class IndicatorDataSourceRecord
    {
        public CalculationSource SourceType { get; internal set; } = CalculationSource.BarData;

        public Indicator? SourceIndicator { get; internal set; }
        public int SourcePlotIndex { get; internal set; } = -1;

        public InstrumentDataSlice? SourceBarData { get; internal set; } = null;

        public DateTime StartDate { get; internal set; }

        public DateTime EndDate { get; internal set; }
    }

    public enum Interval
    {
        Tick,
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

        public TimeSpan GetTimeSpan()
        {
            switch (Type)
            {
                case Interval.Second: return new TimeSpan(0, 0, Value);
                case Interval.Minute: return new TimeSpan(0, Value, 0);
                case Interval.Hour: return new TimeSpan(Value, 0, 0);
                case Interval.Day: return new TimeSpan(Value, 0, 0, 0);
                case Interval.Week: return new TimeSpan(Value * 7, 0, 0, 0);
                //case Interval.Month: return dateTime.AddMonths(Value * n);
                //case Interval.Year: return new TimeSpan(Value, 0, 0, 0); ;
                default:
                    throw new EvolverException($"Unknown interval type in interval.Add() : type={Type}");
            }
        }

        public DateTime Add(DateTime dateTime, int n)
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

        public bool IsFactor(DataInterval subInterval)
        {//is the subInterval and factor of this?

            if (subInterval.Type > Type) return false;

            double thisSpan = GetTimeSpan().TotalSeconds;
            double subSpan = subInterval.GetTimeSpan().TotalSeconds;

            if ((thisSpan % subSpan) == 0) return true;

            return false;
        }

        public long Ticks
        {
            get
            {
                DateTime now = DateTime.Now;
                DateTime then = Add(now, 1);
                return (now - then).Ticks;
            }
        }

        public static int operator /(TimeSpan span, DataInterval interval)
        {
            double n = span / interval.GetTimeSpan();

            return (int)Math.Ceiling(n);
        }

        public static bool operator !=(DataInterval a, DataInterval b)
        {
            return !(a == b);
        }

        public static bool operator ==(DataInterval a, DataInterval b)
        {
            return (a.Type == b.Type && a.Value == b.Value);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is DataInterval)) return false;
            DataInterval b = (DataInterval)obj;
            return this == b;
        }

        public override int GetHashCode()
        {
            string s = Type.ToString() + Value.ToString();
            return s.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Value}{Type.ToString()}";
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

        //public T GetValue(int barsAgo);
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

        public IEnumerable<(T, int)> Select(Func<T, int, (T, int)> selector)
        {
            return _values.Select(selector);
        }
        public IEnumerable<T> Select(Func<T, int, T> selector)
        {
            return _values.Select(selector);
        }

        public IEnumerable<T> GetRange(int start, int count)
        {
            return _values.GetRange(start, count);
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
        //public T GetValue(int barsAgo) { return this[barsAgo]; }
        public T this[int index]
        {
            get
            {
                //int c = _values.Count - 1;
                //if (barsAgo < 0 || barsAgo > c)
                //{
                //    throw new EvolverException();
                //}
                //return _values[c - barsAgo];
                return _values[index];
            }
            internal set { _values[index] = value; }
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

        public static BarDataSeries? RandomSeries(DateTime startTime, DataInterval interval, int size)
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

        public int GetNearestIndexOfTime(DateTime time, int startIndex = 0)
        {
            int iStart = startIndex >= 0 ? startIndex : 0;

            for (int i = iStart; i < Count; i++)
            {
                TimeDataBar iBar = GetValueAt(i);
                if (iBar.Time >= time)
                {
                    if (i == 0 || iBar.Time == time) return i;

                    TimeDataBar prev = GetValueAt(i - 1);
                    TimeSpan prevSpan = time - prev.Time;
                    TimeSpan iSpan = iBar.Time - time;

                    if (prevSpan < iSpan) return i - 1;
                    return i;
                }
            }

            return -1;
        }

        public static InstrumentDataSeries? RandomSeries(Instrument instrument, DateTime startTime, DataInterval interval, int size)
        {
            InstrumentDataSeries barDataSeries = new InstrumentDataSeries();
            barDataSeries.Instrument = instrument;
            barDataSeries.Interval = interval;
            Random r = new Random(DateTime.Now.Second);

            int lastClose = -1;
            for (int i = 0; i < size; i++)
            {
                int open = lastClose == -1 ? r.Next(20, 100) : lastClose;
                int close = r.Next(20, 100);
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


    internal class DataManager : IDisposable
    {
        List<InstrumentDataRecord> _instrumentCache = new List<InstrumentDataRecord>();
        List<Indicator> _indicatorCache = new List<Indicator>();


        public event EventHandler<InstrumentDataRecord>? DataChange = null;

        internal DataManager()
        {
            _indicatorWorker = new Thread(indicatorWorker);
            _indicatorWorker.Name = "DataManager Indicator Runner";
            _indicatorWorker.Start();
        }


        internal void LoadRandomInstrumentRecords()
        {
            InstrumentDataRecord record = new InstrumentDataRecord()
            {
                InstrumentName = "Random",
                Interval = new DataInterval(Interval.Second, 1),
                StartTime = new DateTime(2010, 1, 1, 0, 0, 0),
                EndTime = new DateTime(2026, 1, 1, 0, 0, 0)
            };
            _instrumentCache.Add(record);

            record = new InstrumentDataRecord()
            {
                InstrumentName = "Random",
                Interval = new DataInterval(Interval.Minute, 1),
                StartTime = new DateTime(2010, 1, 1, 0, 0, 0),
                EndTime = new DateTime(2026, 1, 1, 0, 0, 0)
            };
            _instrumentCache.Add(record);

            record = new InstrumentDataRecord()
            {
                InstrumentName = "Random",
                Interval = new DataInterval(Interval.Hour, 1),
                StartTime = new DateTime(2010, 1, 1, 0, 0, 0),
                EndTime = new DateTime(2026, 1, 1, 0, 0, 0)
            };
            _instrumentCache.Add(record);

            record = new InstrumentDataRecord()
            {
                InstrumentName = "Random",
                Interval = new DataInterval(Interval.Day, 1),
                StartTime = new DateTime(2010, 1, 1, 0, 0, 0),
                EndTime = new DateTime(2026, 1, 1, 0, 0, 0)
            };
            _instrumentCache.Add(record);

            record = new InstrumentDataRecord()
            {
                InstrumentName = "Random",
                Interval = new DataInterval(Interval.Week, 1),
                StartTime = new DateTime(2010, 1, 1, 0, 0, 0),
                EndTime = new DateTime(2026, 1, 1, 0, 0, 0)
            };
            _instrumentCache.Add(record);

            record = new InstrumentDataRecord()
            {
                InstrumentName = "Random",
                Interval = new DataInterval(Interval.Month, 1),
                StartTime = new DateTime(2010, 1, 1, 0, 0, 0),
                EndTime = new DateTime(2026, 1, 1, 0, 0, 0)
            };
            _instrumentCache.Add(record);

            record = new InstrumentDataRecord()
            {
                InstrumentName = "Random",
                Interval = new DataInterval(Interval.Year, 1),
                StartTime = new DateTime(2010, 1, 1, 0, 0, 0),
                EndTime = new DateTime(2026, 1, 1, 0, 0, 0)
            };
            _instrumentCache.Add(record);
        }

        public Indicator? CreateIndicator(Type indicatorType, IndicatorProperties properties, Indicator source, CalculationSource sourceType, int sourcePlotIndex = -1)
        {
            if (source.SourceRecord == null)
            {
                Globals.Instance.Log.LogMessage("CreateIndicator failed: source indicator has no record.", LogLevel.Error);
                return null;
            }

            //FIXME : check cache first, only instance if needed
            //foreach (Indicator cachedIndicator in _indicatorCache)
            //{
            //    if (cachedIndicator.IsDataOnly || cachedIndicator.SourceRecord == null) continue;

            //    if (sourceType == CalculationSource.BarData)
            //    {
            //        //if (cachedIndicator.SourceRecord.SourceType != )
            //        //{
            //        //    return cachedIndicator;
            //        //}
            //    }
            //}

            if (indicatorType.BaseType != typeof(Indicator))
            {
                Globals.Instance.Log.LogMessage("CreateIndicator failed: type is not an indicator.", LogLevel.Error);
                return null;
            }

            ConstructorInfo? iConstructor = indicatorType.GetConstructor(new Type[] { typeof(IndicatorProperties) });
            if (iConstructor == null)
            {
                Globals.Instance.Log.LogMessage("CreateIndicator failed: failed to locate constructor.", LogLevel.Error);
                return null;
            }

            Indicator? newIndicator = iConstructor.Invoke(new object[] { properties }) as Indicator;
            if (newIndicator == null)
            {
                Globals.Instance.Log.LogMessage("CreateIndicator failed: constructor failed", LogLevel.Error);
                return null;
            }

            IndicatorDataSourceRecord newSourceRecord = new IndicatorDataSourceRecord();
            newSourceRecord.SourceBarData = source.SourceRecord.SourceBarData;
            newSourceRecord.SourceIndicator = source;
            newSourceRecord.SourcePlotIndex = sourcePlotIndex;
            newSourceRecord.SourceType = sourceType;
            newSourceRecord.StartDate = source.SourceRecord.StartDate;
            newSourceRecord.EndDate = source.SourceRecord.EndDate;

            newIndicator.SetSourceData(newSourceRecord);
            newIndicator.Startup();
            _indicatorCache.Add(newIndicator);

            if (newIndicator.WaitingForDataLoad)
                source.DataChanged += newIndicator.OnSourceDataLoaded;
            else
                IndicatorReadyToRun(newIndicator);

            return newIndicator;
        }

        public Indicator? CreateDataIndicator(Instrument instrument, DataInterval interval, DateTime start, DateTime end)
        {
            InstrumentDataSliceRecord sliceRecord = new InstrumentDataSliceRecord()
            {
                Instrument = instrument,
                Interval = interval,
                StartDate = start,
                EndDate = end
            };

            InstrumentDataSlice? slice = MakeInstrumentSlice(sliceRecord);
            if (slice == null) return null;

            IndicatorDataSourceRecord iSliceRecord = new IndicatorDataSourceRecord()
            {
                SourceBarData = slice,
                SourceType = CalculationSource.BarData,
                StartDate = start,
                EndDate = end
            };

            foreach (Indicator cachedIndicator in _indicatorCache)
            {
                if (!cachedIndicator.IsDataOnly || cachedIndicator.SourceRecord == null) continue;
                if (cachedIndicator.SourceRecord == iSliceRecord)
                {
                    return cachedIndicator;
                }
            }

            Indicator indicator = new Indicator(new IndicatorProperties());
            indicator.Name = instrument.Name;
            indicator.IsDataOnly = true;
            indicator.SetSourceData(iSliceRecord);
            if (indicator.WaitingForDataLoad)
                slice.DataLoaded += indicator.OnSourceDataLoaded;

            indicator.Startup();
            _indicatorCache.Add(indicator);

            if (!indicator.WaitingForDataLoad)
                Globals.Instance.DataManager.IndicatorReadyToRun(indicator);

            return indicator;
        }

        private InstrumentDataSlice? MakeInstrumentSlice(InstrumentDataSliceRecord sliceRecord)
        {
            foreach (InstrumentDataRecord dataRecord in _instrumentCache)
            {
                if (dataRecord.LoadState == DataLoadState.Error) continue;

                if (dataRecord.InstrumentName == sliceRecord.Instrument.Name &&
                    dataRecord.Interval == sliceRecord.Interval)
                {
                    if (dataRecord.StartTime <= sliceRecord.StartDate && dataRecord.EndTime >= sliceRecord.EndDate)
                    {
                        InstrumentDataSlice slice = new InstrumentDataSlice() { Record = sliceRecord };
                        if (dataRecord.Data != null)
                        {
                            slice.SetData(dataRecord);
                            slice.LoadState = DataLoadState.Loaded;
                            return slice;
                        }

                        dataRecord.DataLoaded += slice.OnRawDataLoaded;

                        slice.LoadState = DataLoadState.Loading;
                        dataRecord.LoadState = DataLoadState.Loading;
                        Globals.Instance.DataManager.LoadDataAsync(dataRecord).ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                                dataRecord.FireDataLoadFailed(task.Exception);
                            else
                                dataRecord.FireDataLoadCompleted(slice);
                        }
                        );
                        return slice;
                    }
                }
            }

            return null;
        }

        ////////////////////
        //FIXME : this should be a call dependency graph
        //        that can deliver live data updates in dependency order
        object _indicatorLiveGraphLock = new object();
        Queue<Indicator> _indicatorLiveGraph = new Queue<Indicator>();
        ///////////////////


        object _indicatorReadyToRunQueueLock = new object();
        Queue<Indicator> _indicatorReadyToRunQueue = new Queue<Indicator>();
        Thread _indicatorWorker;
        bool _wantExit = false;
        bool _isSleeping = false;

        private bool disposedValue;

        internal void IndicatorReadyToRun(Indicator indicator)
        {
            lock (_indicatorReadyToRunQueueLock)
            {
                _indicatorReadyToRunQueue.Enqueue(indicator);
            }

            if (_isSleeping) _indicatorWorker.Interrupt();
        }

        private void indicatorWorker()
        {
            try
            {
                while (true)
                {
                    if (_wantExit) break;

                    int queueCount = 0;
                    Indicator? indicator = null;
                    lock (_indicatorReadyToRunQueueLock)
                    {
                        queueCount = _indicatorReadyToRunQueue.Count;
                        if (queueCount > 0)
                            indicator = _indicatorReadyToRunQueue.Dequeue();
                    }

                    if (queueCount == 0)
                    {
                        _isSleeping = true;
                        Thread.Sleep(Timeout.Infinite);
                    }
                    else if (indicator != null)
                    {
                        if (indicator.State != IndicatorState.Startup || indicator.WaitingForDataLoad)
                        {
                            Globals.Instance.Log.LogMessage($"Indicator {indicator.Name} trying to run when not ready", LogLevel.Error);
                            continue;
                        }

                        indicator.RunHistory();

                        lock (_indicatorLiveGraphLock)
                        {
                            _indicatorLiveGraph.Enqueue(indicator);
                        }
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
                _isSleeping = false;
            }
            catch (ThreadAbortException)
            {
                Globals.Instance.Log.LogMessage("DataManager.indicatorWorker thread abort", LogLevel.Info);
            }
            catch (Exception e)
            {
                Globals.Instance.Log.LogMessage("DataManager.indicatorWorker thread exception:", LogLevel.Error);
                Globals.Instance.Log.LogException(e);
            }

        }

        internal async Task<InstrumentDataRecord> LoadDataAsync(InstrumentDataRecord dataRecord)
        {
            if (dataRecord.InstrumentName == "Random")
            {//special handling here...
                Instrument? instrument = Globals.Instance.InstrumentCollection.Lookup("Random");
                if (instrument == null)
                    throw new EvolverException($"Unknown Instrument: Random");

                await Task.Delay(2000);//<-- fake 2 sec delay for testing 

                ///////////////////////////
                TimeSpan span = dataRecord.EndTime - dataRecord.StartTime;

                int n = 0;
                if (dataRecord.Interval.Type == Interval.Year)
                {
                    n = dataRecord.EndTime.Year - dataRecord.StartTime.Year;
                }
                else if (dataRecord.Interval.Type == Interval.Month)
                {
                    n = ((dataRecord.EndTime.Year - dataRecord.StartTime.Year) * 12) +
                        dataRecord.EndTime.Month - dataRecord.StartTime.Month;
                }
                else
                    n = span / dataRecord.Interval;

                InstrumentDataSeries? series = InstrumentDataSeries.RandomSeries(instrument, dataRecord.StartTime, dataRecord.Interval, n);
                if (series == null)
                    throw new EvolverException($"Unable to generate random data.");
                ///////////////////////////


                dataRecord.Data = series;
            }

            return dataRecord;
        }

        public void OnConnectionDataUpdate(object? sender, ConnectionDataUpdateEventArgs e)
        {

        }

        internal void Shutdown()
        {
            _wantExit = true;
            if (_isSleeping && _indicatorWorker.IsAlive) _indicatorWorker.Interrupt();

            if (_indicatorWorker.IsAlive)
            {
                if (!_indicatorWorker.Join(TimeSpan.FromSeconds(3)))
                {
                    Globals.Instance.Log.LogMessage("DataManager.indicatorWorker failed to shutdown.", LogLevel.Error);
                }
            }
            else
            {
                Globals.Instance.Log.LogMessage("DataManager.indicatorWorker was already terminated.", LogLevel.Warn);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Shutdown();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    internal class DataGraph
    {
        object _lock = new object();

        internal List<DataGraphNode> RootNodes { get; private set; } = new List<DataGraphNode>();

        internal DataGraph() { }

        internal void OnDataUpdate(object? sender, ConnectionDataUpdateEventArgs args)
        {
            lock (_lock)
            {
                List<DataGraphNode> relevantRootNodes = FindAllDataNodes(args.Instrument);
                foreach (DataGraphNode node in relevantRootNodes)
                {
                    //TODO: deliver data update, breadth first
                }
            }
        }

        internal bool AddIndicator(Indicator indicator)
        {
            lock (_lock)
            {
                DataGraphNode? node = FindNode(indicator);
                if (node != null) return true;//node already exists...

                if (indicator.IsDataOnly)
                {//data only nodes are always root nodes...
                    DataGraphNode newNode = new DataGraphNode(this, indicator);
                    RootNodes.Add(newNode);
                    return true;
                }
                else
                {
                    DataGraphNode newNode = new DataGraphNode(this, indicator);

                    foreach (BarsPointer bars in indicator.Bars)
                    {

                        DataGraphNode? parentNode = FindDataNode(bars.Record.Instrument, bars.Record.Interval);
                        if (parentNode == null)
                        {
                            Globals.Instance.Log.LogMessage("", LogLevel.Error);
                            return false;
                        }

                        newNode.Parents.Add(parentNode);
                        parentNode.Children.Add(newNode);
                    }

                    foreach (InputIndicator input in indicator.Inputs)
                    {
                        DataGraphNode? parentNode = FindNode(input.Indicator);
                        if (parentNode == null)
                        {
                            Globals.Instance.Log.LogMessage("", LogLevel.Error);
                            return false;
                        }

                        newNode.Parents.Add(parentNode);
                        parentNode.Children.Add(newNode);
                    }

                    return true;
                }
            }
        }

        List<DataGraphNode> FindAllDataNodes(Instrument instrument)
        {
            List<DataGraphNode> rootNodes = new List<DataGraphNode>();

            lock (_lock)
            {
                foreach (DataGraphNode node in RootNodes)
                {
                    if (node.Indicator.SourceRecord != null &&
                        node.Indicator.SourceRecord.SourceBarData != null &&
                        node.Indicator.SourceRecord.SourceBarData.Record.Instrument == instrument
                        )
                        rootNodes.Add(node);
                }

                return rootNodes;
            }
        }

        DataGraphNode? FindDataNode(Instrument instrument, DataInterval interval)
        {
            lock (_lock)
            {
                foreach (DataGraphNode node in RootNodes)
                {
                    if (node.Indicator.SourceRecord != null &&
                        node.Indicator.SourceRecord.SourceBarData != null &&
                        node.Indicator.SourceRecord.SourceBarData.Record.Instrument == instrument &&
                        node.Indicator.Interval == interval)
                        return node;
                }
                return null;
            }
        }

        internal DataGraphNode? FindNode(Indicator indicator)
        {
            lock (_lock)
            {
                foreach (DataGraphNode node in RootNodes)
                {
                    if (node.Indicator == indicator) return node;
                    if (indicator.IsDataOnly) continue;

                    DataGraphNode? childNode = node.FindChildNode(indicator);
                    if (childNode != null) return childNode;
                }
                return null;
            }
        }
    }

    internal class DataGraphNode
    {
        internal DataGraphNode(DataGraph graph, Indicator indicator) { _parentGraph = graph; Indicator = indicator; }

        private DataGraph _parentGraph;
        internal Indicator Indicator { get; private set; }

        internal List<DataGraphNode> Parents { get; private set; } = new List<DataGraphNode>();
        internal List<DataGraphNode> Children { get; private set; } = new List<DataGraphNode>();

        internal DataGraphNode? FindChildNode(Indicator indicator)
        {
            foreach (DataGraphNode node in Children)
            {
                if (node.Indicator == indicator) return node;

                DataGraphNode? childNode = node.FindChildNode(indicator);
                if (childNode != null) return childNode;
            }

            return null;
        }
    }
}