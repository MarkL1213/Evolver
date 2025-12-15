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

namespace EvolverCore
{
    public enum Interval
    {
        Second,
        Minute,
        Hour,
        Day,
        Week
    }

    public enum BarPointValue
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

        public DateTime X { get { return Time; } }
        public double Y { get { return Value; } }

        public static TimeDataPoint Empty
        {
            get
            {
                return new TimeDataPoint() { Time = DateTime.MinValue, Value = 0 };
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

        public int Count { get { return _values.Count; } }

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
        DataInterval Interval { get; set; }

        public TimeDataSeries()
        {
            TimeZoneInfo = TimeZoneInfo.Local;
        }

    }

    public record BarPricePoint : IDataPoint
    {
        private readonly TimeDataBar _bar;  // Shared reference — no copy
        private readonly BarPointValue _field;

        public BarPricePoint(TimeDataBar? bar, BarPointValue field)
        {
            _bar = bar ?? throw new ArgumentNullException(nameof(bar));
            _field = field;
        }

        public DateTime X => _bar.Time;

        public double Y => _field switch
        {
            BarPointValue.Open => _bar.Open,
            BarPointValue.High => _bar.High,
            BarPointValue.Low => _bar.Low,
            BarPointValue.Close => _bar.Close,
            BarPointValue.Bid => _bar.Bid,
            BarPointValue.Ask => _bar.Ask,
            BarPointValue.Volume => _bar.Volume,
            BarPointValue.HL => (_bar.High + _bar.Low) / 2,
            BarPointValue.OC => (_bar.Open + _bar.Close) / 2,
            BarPointValue.HLC => (_bar.High + _bar.Low + _bar.Close) / 3,
            BarPointValue.OHLC => (_bar.Open + _bar.High + _bar.Low + _bar.Close) / 4,
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
        DataInterval Interval { get; set; }

        public BarDataSeries()
        {
            TimeZoneInfo = TimeZoneInfo.Local;
        }

        internal BarPointValue ValueType { set; get; } = BarPointValue.Close;

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
}
