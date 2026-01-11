using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace EvolverCore
{
    public enum InstrumentType
    {
        Futures
    }

    public class InstrumentCollection
    {
        Dictionary<string, Instrument> _instruments = new Dictionary<string, Instrument>();

        public InstrumentCollection() { }

        internal void LoadRandomInstrument()
        {
            Instrument i = new Instrument();
            i.Name = "Random";
            _instruments.Add(i.Name, i);
        }

        public Instrument? Lookup(string name)
        {
            if (string.IsNullOrEmpty(name)) { return null; }
            if (!_instruments.ContainsKey(name)) { return null; }
            return _instruments[name];
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


    public class InstrumentDataLoadedEventArgs
    {
        public InstrumentDataLoadedEventArgs(InstrumentDataSlice slice) { Exception = null; Slice = slice; }
        public InstrumentDataLoadedEventArgs(Exception? e = null) { Exception = e; Slice = null; }
        public Exception? Exception { private set; get; }
        public InstrumentDataSlice? Slice { private set; get; }
    }

    public class InstrumentDataRecord
    {
        public string InstrumentName { get; internal set; } = string.Empty;

        public DataInterval Interval { get; internal set; } = new DataInterval(EvolverCore.Interval.Minute, 1);
        public DateTime StartTime { get; internal set; }
        public DateTime EndTime { get; internal set; }

        public DataLoadState LoadState { get; internal set; } = DataLoadState.NotLoaded;

        public event EventHandler<InstrumentDataLoadedEventArgs>? DataLoaded;

        internal void FireDataLoadFailed(Exception e)
        {
            LoadState = DataLoadState.Error;
            DataLoaded?.Invoke(this, new InstrumentDataLoadedEventArgs(e));
        }
        internal void FireDataLoadCompleted(InstrumentDataSlice slice)
        {
            LoadState = DataLoadState.Loaded;
            DataLoaded?.Invoke(this, new InstrumentDataLoadedEventArgs(slice));
        }

        public string FileName { get; internal set; } = string.Empty;

        public InstrumentDataSeries? Data { get; internal set; } = null;
    }
}
