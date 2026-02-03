using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EvolverCore.Models
{
    public class InstrumentDataRecord_v2
    {
        public InstrumentDataRecord_v2(Instrument instrument, DataInterval interval)
        {
            Instrument = instrument;
            Interval = interval;
            MinTime = DateTime.MinValue;
            MaxTime = DateTime.MinValue;
        }
        
        public InstrumentDataRecord_v2(Instrument instrument, DataInterval interval, DateTime min, DateTime max)
        {
            Instrument = instrument;
            Interval = interval;
            MinTime = min;
            MaxTime = max;
        }
        public Instrument Instrument { get; init; }
        public DataInterval Interval { get; init; }

        public DateTime MinTime { get; internal set; }
        public DateTime MaxTime { get; internal set; }

        internal bool IsContiguous(InstrumentDataRecord_v2 b)
        {
            return Interval.Add(MaxTime, 1) == b.MinTime;
        }

        internal void Append(InstrumentDataRecord_v2 b)
        {
            MaxTime = b.MaxTime;
        }
    }

    public class DataTableRecordCollection
    {
        internal DataTableRecordCollection()
        {
        }

        object _lock = new object();
        Dictionary<string, Dictionary<string, List<InstrumentDataRecord_v2>>> _records = new Dictionary<string, Dictionary<string, List<InstrumentDataRecord_v2>>>();


        internal async Task LoadAvailableInstrumentData()
        {
            Dictionary<string, Dictionary<string, List<InstrumentDataRecord_v2>>> newRecords = new Dictionary<string, Dictionary<string, List<InstrumentDataRecord_v2>>>();

            DirectoryInfo barDataDir = new DirectoryInfo(Path.Combine(Globals.Instance.DataDirectory, "bars"));

            if (!barDataDir.Exists) return;

            IEnumerable<DirectoryInfo> instrumentDirs = barDataDir.EnumerateDirectories();
            foreach (DirectoryInfo instrumentDir in instrumentDirs)
            {
                IEnumerable<DirectoryInfo> intervalDirs = instrumentDir.EnumerateDirectories();

                string instrumentKey = instrumentDir.Name;
                Instrument? instrument = Globals.Instance.InstrumentCollection.Lookup(instrumentKey);
                if (instrument == null)
                    throw new EvolverException($"Unable to lookup instrument from folder name {instrumentKey}.");

                Dictionary<string, List<InstrumentDataRecord_v2>> intervalDict;
                if (newRecords.ContainsKey(instrumentKey))
                    intervalDict = newRecords[instrumentKey];
                else
                {
                    intervalDict = new Dictionary<string, List<InstrumentDataRecord_v2>>();
                    newRecords.Add(instrumentKey, intervalDict);
                }

                foreach (DirectoryInfo intervalDir in intervalDirs)
                {
                    IEnumerable<FileInfo> files = intervalDir.EnumerateFiles("*.parquet");

                    string intervalKey = intervalDir.Name;
                    DataInterval? interval = DataInterval.TryParseString(intervalKey);
                    if (!interval.HasValue)
                        throw new EvolverException($"Unable to parse interval folder name {intervalKey} in the instrument {instrumentKey} folder.");

                    List<InstrumentDataRecord_v2> dataRecords;
                    if (intervalDict.ContainsKey(intervalKey))
                        dataRecords = intervalDict[intervalKey];
                    else
                    {
                        dataRecords = new List<InstrumentDataRecord_v2>();
                        intervalDict.Add(intervalKey, dataRecords);
                    }

                    foreach (FileInfo file in files)
                    {
                        (DateTime min, DateTime max) = await loadTimestampRange(file.FullName);
                        InstrumentDataRecord_v2 newRecord = new InstrumentDataRecord_v2(instrument, interval.Value, min, max);

                        bool found = false;
                        foreach (InstrumentDataRecord_v2 existingRecord in dataRecords)
                        {
                            if (existingRecord.IsContiguous(newRecord))
                            {
                                existingRecord.Append(newRecord);
                                found = true;
                                break;
                            }
                        }
                        if(!found) dataRecords.Add(newRecord);
                    }
                }
            }

            lock (_records) { _records = newRecords; }
        }

        private async Task<(DateTime Min, DateTime Max)> loadTimestampRange(string filePath)
        {
            using Stream stream = File.OpenRead(filePath);
            using ParquetReader reader = await ParquetReader.CreateAsync(stream);

            DataField? timestampField = reader.Schema.DataFields.FirstOrDefault(x => x.Name == "Time");
            if (timestampField == null)
                throw new EvolverException($"In data file {filePath} there is no 'Time' column.");

            DateTime overallMin = DateTime.MaxValue;
            DateTime overallMax = DateTime.MinValue;

            for (int rg = 0; rg < reader.RowGroupCount; rg++)
            {
                using ParquetRowGroupReader groupReader = reader.OpenRowGroupReader(rg);

                DataColumnStatistics? stats = groupReader.GetStatistics(timestampField);
                if (stats == null)
                    throw new EvolverException($"In data file {filePath} there are no statistics available for 'Time' column");

                DateTime? min = (DateTime?)stats.MinValue;
                DateTime? max = (DateTime?)stats.MaxValue;

                if (!min.HasValue || min == DateTime.MinValue || !max.HasValue || max == DateTime.MinValue)
                    throw new EvolverException($"In data file {filePath} 'Time' column statistics missing one or both Min/Max values.");

                overallMin = min < overallMin ? (DateTime)min : overallMin;
                overallMax = max > overallMax ? (DateTime)max : overallMax;
            }

            if (overallMin == DateTime.MaxValue || overallMax == DateTime.MinValue)
                throw new EvolverException($"In data file {filePath} there were no valid timestamps found");

            return (overallMin, overallMax);
        }
    }

    public class DataTableManager : IDisposable
    {
        DataWarehouse _dataWarehouse;
        DataDepGraph _dataDepGraph;

        private object _handlerLock = new object();
        private Dictionary<Connection, EventHandler<ConnectionDataUpdateEventArgs>> _connectionDataUpdateHandlers = new Dictionary<Connection, EventHandler<ConnectionDataUpdateEventArgs>>();
        private BlockingCollection<Action<CancellationToken>> _dataUpdateQueue = new BlockingCollection<Action<CancellationToken>>();
        private bool _disposedValue = false;
        private bool _isShutdown = false;
        private Thread _connectionDataUpdateQueueWorker;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        
        //internal event EventHandler<InstrumentDataRecord>? DataChange = null;
        internal event EventHandler? DataUpdateWorkerError = null;

        internal DataWarehouse DataWarehouse { get { return _dataWarehouse; } }
        internal DataDepGraph DepGraph { get { return _dataDepGraph; } }

        internal DataTableManager()
        {
            _dataWarehouse = new DataWarehouse(this);
            _dataDepGraph = new DataDepGraph();

            _connectionDataUpdateQueueWorker = new Thread(connectionDataUpdateQueueWorker);
            _connectionDataUpdateQueueWorker.Name = "DataTableManager Update Worker";
            _connectionDataUpdateQueueWorker.IsBackground = true;
        }



        private void connectionDataUpdateQueueWorker()
        {
            CancellationToken token = _cts.Token;

            try
            {
                while (true)
                {
                    Action<CancellationToken> action;
                    try
                    {
                        action = _dataUpdateQueue.Take(token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (InvalidOperationException) { break; }

                    try
                    {
                        action(token);
                    }
                    catch (Exception e)
                    {
                        Globals.Instance.Log.LogMessage("DataTableManager Update Worker event exception:", LogLevel.Error);
                        Globals.Instance.Log.LogException(e);
                    }
                }
            }
            catch (Exception e)
            {
                Globals.Instance.Log.LogMessage("DataTableManager Update Worker thread exception:", LogLevel.Error);
                Globals.Instance.Log.LogException(e);

                DataUpdateWorkerError?.Invoke(this, EventArgs.Empty);
            }
        }

        public void OnConnectionDataUpdate(object? sender, ConnectionDataUpdateEventArgs e, CancellationToken token)
        {
            //TODO: Handle the connection data update event
            _dataWarehouse.DataUpdate(e);
        }

        
        public void UnsubscribeFromConnection(Connection c)
        {
            lock (_handlerLock)
            {
                if (_connectionDataUpdateHandlers.ContainsKey(c))
                {
                    c.DataUpdate -= _connectionDataUpdateHandlers[c];
                    _connectionDataUpdateHandlers.Remove(c);
                }
            }
        }

        public void SubscribeToConnection(Connection c)
        {
            lock (_handlerLock)
            {
                if (_isShutdown) return;

                if (!_connectionDataUpdateQueueWorker.IsAlive) _connectionDataUpdateQueueWorker.Start();

                UnsubscribeFromConnection(c);

                EventHandler<ConnectionDataUpdateEventArgs> handler = (sender, args) => { _dataUpdateQueue.Add((token) => OnConnectionDataUpdate(sender, args, token)); };
                _connectionDataUpdateHandlers.Add(c, handler);
                c.DataUpdate += handler;
            }
        }

        internal void Shutdown()
        {
            lock (_handlerLock)
            {
                _isShutdown = true;

                foreach (Connection c in _connectionDataUpdateHandlers.Keys.ToArray())
                    UnsubscribeFromConnection(c);
                _connectionDataUpdateHandlers.Clear();
            }

            _dataUpdateQueue.CompleteAdding();

            if (_connectionDataUpdateQueueWorker.IsAlive)
            {
                _cts.Cancel(); // Trigger cancellation
                if (!_connectionDataUpdateQueueWorker.Join(TimeSpan.FromSeconds(10)))
                {
                    Globals.Instance.Log.LogMessage("DataTableManager Update Worker failed to shutdown.", LogLevel.Error);
                    //move on, don't care anymore background thread will terminate on exit anyway
                }
            }
            else
            {
                Globals.Instance.Log.LogMessage("DataTableManager Update Worker was already terminated.", LogLevel.Warn);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Shutdown();
                    _dataWarehouse.Shutdown();
                    _cts.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
