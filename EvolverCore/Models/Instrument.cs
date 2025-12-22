using System.Xml.Serialization;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Collections;

namespace EvolverCore
{
    public enum InstrumentType
    {
        Futures
    }

    public class InstrumentCollection : IDictionary<string, Instrument>
    {
        Dictionary<string,Instrument> _instruments = new Dictionary<string, Instrument> ();

        public Instrument this[string key] { get => ((IDictionary<string, Instrument>)_instruments)[key]; set => ((IDictionary<string, Instrument>)_instruments)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, Instrument>)_instruments).Keys;

        public ICollection<Instrument> Values => ((IDictionary<string, Instrument>)_instruments).Values;

        public int Count => ((ICollection<KeyValuePair<string, Instrument>>)_instruments).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, Instrument>>)_instruments).IsReadOnly;

        public void Add(string key, Instrument value)
        {
            ((IDictionary<string, Instrument>)_instruments).Add(key, value);
        }

        public void Add(KeyValuePair<string, Instrument> item)
        {
            ((ICollection<KeyValuePair<string, Instrument>>)_instruments).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, Instrument>>)_instruments).Clear();
        }

        public bool Contains(KeyValuePair<string, Instrument> item)
        {
            return ((ICollection<KeyValuePair<string, Instrument>>)_instruments).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, Instrument>)_instruments).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, Instrument>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, Instrument>>)_instruments).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, Instrument>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, Instrument>>)_instruments).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, Instrument>)_instruments).Remove(key);
        }

        public bool Remove(KeyValuePair<string, Instrument> item)
        {
            return ((ICollection<KeyValuePair<string, Instrument>>)_instruments).Remove(item);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out Instrument value)
        {
            return ((IDictionary<string, Instrument>)_instruments).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_instruments).GetEnumerator();
        }
    }

    public class Instrument
    {
        public string Name { internal set; get; }

        public string Description { internal set; get; }

        public string SessionHoursName { internal set; get; }

        [XmlIgnore]
        public SessionHours SessionHours { get; }

        public InstrumentType Type { internal set; get; }

        public Instrument()
        {
            Name = string.Empty;
            Description = string.Empty;
            SessionHoursName = string.Empty;
            SessionHours = new SessionHours();
            Type = InstrumentType.Futures;
        }



    }

    public class InstrumentDataRecordCollection : IDictionary<string, List<InstrumentDataRecord>>
    {
        Dictionary<string, List<InstrumentDataRecord>> _collection = new Dictionary<string, List<InstrumentDataRecord>>();

        public List<InstrumentDataRecord> this[string key] { get => ((IDictionary<string, List<InstrumentDataRecord>>)_collection)[key]; set => ((IDictionary<string, List<InstrumentDataRecord>>)_collection)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, List<InstrumentDataRecord>>)_collection).Keys;

        public ICollection<List<InstrumentDataRecord>> Values => ((IDictionary<string, List<InstrumentDataRecord>>)_collection).Values;

        public int Count => ((ICollection<KeyValuePair<string, List<InstrumentDataRecord>>>)_collection).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, List<InstrumentDataRecord>>>)_collection).IsReadOnly;

        public void Add(string key, List<InstrumentDataRecord> value)
        {
            ((IDictionary<string, List<InstrumentDataRecord>>)_collection).Add(key, value);
        }

        public void Add(KeyValuePair<string, List<InstrumentDataRecord>> item)
        {
            ((ICollection<KeyValuePair<string, List<InstrumentDataRecord>>>)_collection).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, List<InstrumentDataRecord>>>)_collection).Clear();
        }

        public bool Contains(KeyValuePair<string, List<InstrumentDataRecord>> item)
        {
            return ((ICollection<KeyValuePair<string, List<InstrumentDataRecord>>>)_collection).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, List<InstrumentDataRecord>>)_collection).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, List<InstrumentDataRecord>>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, List<InstrumentDataRecord>>>)_collection).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, List<InstrumentDataRecord>>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, List<InstrumentDataRecord>>>)_collection).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, List<InstrumentDataRecord>>)_collection).Remove(key);
        }

        public bool Remove(KeyValuePair<string, List<InstrumentDataRecord>> item)
        {
            return ((ICollection<KeyValuePair<string, List<InstrumentDataRecord>>>)_collection).Remove(item);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out List<InstrumentDataRecord> value)
        {
            return ((IDictionary<string, List<InstrumentDataRecord>>)_collection).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_collection).GetEnumerator();
        }
    }

    public class InstrumentDataRecord
    {
        public string InstrumentName { get; internal set; } = string.Empty;
        public long StartTime { get; internal set; }
        public long EndTime { get; internal set; }

        public string FileName { get;internal set; } = string.Empty;

        public DataInterval Interval { get; internal set; } = new DataInterval(EvolverCore.Interval.Minute, 1);

        public InstrumentDataSeries? Data { get; internal set; } = null;
    }
}
