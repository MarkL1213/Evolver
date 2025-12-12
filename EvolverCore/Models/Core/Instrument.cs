using System.Xml.Serialization;
using System.Collections.Generic;
using EvolverCore.Session;
using System.Diagnostics.CodeAnalysis;
using System.Collections;



namespace EvolverAPI.Instrument
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

    public class InstrumentDataInfoCollection : IDictionary<string, List<InstrumentDataInfo>>
    {
        Dictionary<string, List<InstrumentDataInfo>> _collection = new Dictionary<string, List<InstrumentDataInfo>>();

        public List<InstrumentDataInfo> this[string key] { get => ((IDictionary<string, List<InstrumentDataInfo>>)_collection)[key]; set => ((IDictionary<string, List<InstrumentDataInfo>>)_collection)[key] = value; }

        public ICollection<string> Keys => ((IDictionary<string, List<InstrumentDataInfo>>)_collection).Keys;

        public ICollection<List<InstrumentDataInfo>> Values => ((IDictionary<string, List<InstrumentDataInfo>>)_collection).Values;

        public int Count => ((ICollection<KeyValuePair<string, List<InstrumentDataInfo>>>)_collection).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, List<InstrumentDataInfo>>>)_collection).IsReadOnly;

        public void Add(string key, List<InstrumentDataInfo> value)
        {
            ((IDictionary<string, List<InstrumentDataInfo>>)_collection).Add(key, value);
        }

        public void Add(KeyValuePair<string, List<InstrumentDataInfo>> item)
        {
            ((ICollection<KeyValuePair<string, List<InstrumentDataInfo>>>)_collection).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, List<InstrumentDataInfo>>>)_collection).Clear();
        }

        public bool Contains(KeyValuePair<string, List<InstrumentDataInfo>> item)
        {
            return ((ICollection<KeyValuePair<string, List<InstrumentDataInfo>>>)_collection).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, List<InstrumentDataInfo>>)_collection).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, List<InstrumentDataInfo>>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, List<InstrumentDataInfo>>>)_collection).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, List<InstrumentDataInfo>>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, List<InstrumentDataInfo>>>)_collection).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, List<InstrumentDataInfo>>)_collection).Remove(key);
        }

        public bool Remove(KeyValuePair<string, List<InstrumentDataInfo>> item)
        {
            return ((ICollection<KeyValuePair<string, List<InstrumentDataInfo>>>)_collection).Remove(item);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out List<InstrumentDataInfo> value)
        {
            return ((IDictionary<string, List<InstrumentDataInfo>>)_collection).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_collection).GetEnumerator();
        }
    }

    public class InstrumentDataInfo
    {
        public string InstrumentName { get; internal set; }
        public long StartTime { get; internal set; }
        public long EndTime { get; internal set; }
    }
}
