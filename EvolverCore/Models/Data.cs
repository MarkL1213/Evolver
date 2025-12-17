using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using MessagePack;
using Microsoft.VisualBasic;

namespace EvolverCore
{
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

        public DataInterval(Interval type, int value) { Type = type;Value = value; }
    }

    public record TimeDataBar : IDataPoint
    {
        public TimeDataBar(DateTime time,double open, double high, double low, double close, long volume, double bid,double ask)
        {
            Time = DateTime.SpecifyKind(time,time.Kind);
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

    public interface IDataPoint
    {
        public DateTime X { get; }
        public double Y { get; }
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

    public class DataSeries
    {
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

        public IEnumerable<T> Select(Func<T,int,T> selector)
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

        public IEnumerable<T> SkipWhile(Func<T,bool> skipper)
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
                    while(br.BaseStream.Position < br.BaseStream.Length)
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
        private readonly TimeDataBar _bar;  // Shared reference — no copy
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
    }

    public class InstrumentBarDataSeries : BarDataSeries
    {
        public Instrument? Instrument { get; internal set; }

    }



    public abstract class NewSeriesBase
    {
    }

    public class NewSeries<T> : NewSeriesBase
    {
        private List<T> _values = new List<T> ();

        public DataInterval Interval { get; internal set; }
        public int Count { get { return _values.Count; } }

        public void Add(T item) { _values.Add(item); }
        public void Clear() { _values.Clear(); }

        public IEnumerator<T> GetEnumerator() { return _values.GetEnumerator(); }

        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            return _values.Where(predicate);
        }

        public IEnumerable<T> Select(Func<T, int, T> selector)
        {
            return _values.Select(selector);
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

        public T? Min(Func<T, T> selector)
        {
            return _values.Min(selector);
        }

        public T? Max(Func<T, T> selector)
        {
            return _values.Max(selector);
        }

        public bool IsDataValid(int barsAgo)
        {
            int c = _values.Count - 1;
            if (barsAgo < 0 || barsAgo >= c)
                return false;

            return !(this[barsAgo] is double.NaN);
        }
        public bool IsDataValidAt(int index)
        {
            if (index < 0 || index >= _values.Count)
                return false;

            return !(_values[index] is double.NaN);
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
        }


    }
    public class NewBarSeries
    {
        NewSeries<DateTime> _times = new NewSeries<DateTime> ();
        List<NewSeriesBase> _barValues = new List<NewSeriesBase> ();

        public NewBarSeries()
        {
            _barValues.Add(new NewSeries<double>());
            _barValues.Add(new NewSeries<double>());
            _barValues.Add(new NewSeries<double>());
            _barValues.Add(new NewSeries<double>());
            _barValues.Add(new NewSeries<long>());
            _barValues.Add(new NewSeries<double>());
            _barValues.Add(new NewSeries<double>());
        }

        public void AddNewBar(DateTime time)
        {
            _times.Add(time);
            Open.Add(double.NaN);
            High.Add(double.NaN);
            Low.Add(double.NaN);
            Close.Add(double.NaN);
            Volume.Add(0);
            Bid.Add(double.NaN);
            Ask.Add(double.NaN);
        }

        public double PriceFieldValueAt(BarPriceValue priceField, int index)
        {
            switch (priceField)
            {
                case BarPriceValue.Open: return Open.GetValueAt(index);
                case BarPriceValue.High: return High.GetValueAt(index);
                case BarPriceValue.Low: return Low.GetValueAt(index);
                case BarPriceValue.Close: return Close.GetValueAt(index);
                case BarPriceValue.Bid: return Bid.GetValueAt(index);
                case BarPriceValue.Ask: return Ask.GetValueAt(index);
                case BarPriceValue.Volume: return Volume.GetValueAt(index);
                case BarPriceValue.HL: return (High.GetValueAt(index) + Low.GetValueAt(index)) / 2;
                case BarPriceValue.OC: return (Open.GetValueAt(index) + Close.GetValueAt(index)) / 2;
                case BarPriceValue.HLC: return (High.GetValueAt(index) + Low.GetValueAt(index) + Close.GetValueAt(index)) / 3;
                case BarPriceValue.OHLC: return (Open.GetValueAt(index) + High.GetValueAt(index) + Low.GetValueAt(index) + Close.GetValueAt(index)) / 4;
                default:
                    throw new NotSupportedException($"Unsupported PriceField: {priceField}");
            }
        }
        public double PriceFieldValue(BarPriceValue priceField, int barsAgo)
        {
            switch (priceField)
            {
                case BarPriceValue.Open:   return Open[barsAgo];
                case BarPriceValue.High:   return High[barsAgo];
                case BarPriceValue.Low:    return Low[barsAgo];
                case BarPriceValue.Close:  return Close[barsAgo];
                case BarPriceValue.Bid:    return Bid[barsAgo];
                case BarPriceValue.Ask:    return Ask[barsAgo];
                case BarPriceValue.Volume: return Volume[barsAgo];
                case BarPriceValue.HL:     return (High[barsAgo] + Low[barsAgo]) / 2;
                case BarPriceValue.OC:     return (Open[barsAgo] + Close[barsAgo]) / 2;
                case BarPriceValue.HLC:    return (High[barsAgo] + Low[barsAgo] + Close[barsAgo]) / 3;
                case BarPriceValue.OHLC:   return (Open[barsAgo] + High[barsAgo] + Low[barsAgo] + Close[barsAgo]) / 4;
                default:
                    throw new NotSupportedException($"Unsupported PriceField: {priceField}");
            }
        }

        public NewSeries<DateTime> Time { get { return _times; } }
        public NewSeries<double> Open { get { return (NewSeries<double>)_barValues[0]; } }
        public NewSeries<double> High { get { return (NewSeries<double>)_barValues[1]; } }
        public NewSeries<double> Low { get { return (NewSeries<double>)_barValues[2]; } }
        public NewSeries<double> Close { get { return (NewSeries<double>)_barValues[3]; } }
        public NewSeries<long> Volume { get { return (NewSeries<long>)_barValues[4]; } }
        public NewSeries<double> Bid { get { return (NewSeries<double>)_barValues[5]; } }
        public NewSeries<double> Ask { get { return (NewSeries<double>)_barValues[6]; } }
    }
}
