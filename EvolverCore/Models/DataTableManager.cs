using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System;
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

            IEnumerable<DirectoryInfo> instrumentDirs = barDataDir.EnumerateDirectories();
            foreach (DirectoryInfo instrumentDir in instrumentDirs)
            {
                IEnumerable<DirectoryInfo> intervalDirs = instrumentDir.EnumerateDirectories();

                string instrumentKey = instrumentDir.Name;//FIXME: this probably needs some path extraction
                Instrument? instrument = Globals.Instance.InstrumentCollection.Lookup(instrumentKey);
                if (instrument == null)
                    throw new EvolverException("");

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

                    string intervalKey = intervalDir.Name;//FIXME: this probably needs some path extraction
                    DataInterval? interval = DataInterval.TryParseString(intervalKey);
                    if (!interval.HasValue)
                        throw new EvolverException("");

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

                DateTime? min = (DateTime?)stats.MinValue; // Cast based on your type
                DateTime? max = (DateTime?)stats.MaxValue;

                if (!min.HasValue || !max.HasValue)
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
        private DataTableRecordCollection _recordCollection = new DataTableRecordCollection();
        private DataDepGraph _depGraph = new DataDepGraph();
        
        List<BarTable> _Tables = new List<BarTable>();

        Thread _tableManagerWorker;
        bool _wantExit = false;
        bool _isSleeping = false;
        private bool disposedValue;

        public event EventHandler<InstrumentDataRecord>? DataChange = null;

        internal DataTableManager()
        {
            _tableManagerWorker = new Thread(indicatorWorker);
            _tableManagerWorker.Name = "DataManager Indicator Runner";
            _tableManagerWorker.Start();
        }

        internal async Task LoadRecords()
        {
            await _recordCollection.LoadAvailableInstrumentData();
        }


        private void indicatorWorker()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        if (_wantExit) break;

                        int queueCount = 0;
                        //dequeue next job/event

                        if (queueCount == 0)
                        {
                            _isSleeping = true;
                            Thread.MemoryBarrier();

                            Thread.Sleep(Timeout.Infinite);
                        }
                        else
                        {
                            //do stuff..
                        }
                    }
                    catch (ThreadInterruptedException)
                    {
                        _isSleeping = false;
                    }
                }
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

        internal async Task<DataTable> LoadDataAsync(InstrumentDataRecord dataRecord)
        {
            //if (dataRecord.InstrumentName == "Random")
            //{//special handling here...
            //    Instrument? instrument = Globals.Instance.InstrumentCollection.Lookup("Random");
            //    if (instrument == null)
            //        throw new EvolverException($"Unknown Instrument: Random");

            //    await Task.Delay(2000);//<-- fake 2 sec delay for testing 

            //    ///////////////////////////
            //    TimeSpan span = dataRecord.EndTime - dataRecord.StartTime;

            //    int n = 0;
            //    if (dataRecord.Interval.Type == Interval.Year)
            //    {
            //        n = dataRecord.EndTime.Year - dataRecord.StartTime.Year;
            //    }
            //    else if (dataRecord.Interval.Type == Interval.Month)
            //    {
            //        n = ((dataRecord.EndTime.Year - dataRecord.StartTime.Year) * 12) +
            //            dataRecord.EndTime.Month - dataRecord.StartTime.Month;
            //    }
            //    else
            //        n = span / dataRecord.Interval;

            //    InstrumentDataSeries? series = InstrumentDataSeries.RandomSeries(instrument, dataRecord.StartTime, dataRecord.Interval, n);
            //    if (series == null)
            //        throw new EvolverException($"Unable to generate random data.");
            //    ///////////////////////////


            //    dataRecord.Data = series;
            //}

            //return dataRecord;

            return new DataTable(new ParquetSchema(new List<DataField> { }), 0);
        }

        public void OnConnectionDataUpdate(object? sender, ConnectionDataUpdateEventArgs e)
        {

        }

        internal void Shutdown()
        {
            _wantExit = true;
            if (_isSleeping && _tableManagerWorker.IsAlive) _tableManagerWorker.Interrupt();

            if (_tableManagerWorker.IsAlive)
            {
                if (!_tableManagerWorker.Join(TimeSpan.FromSeconds(3)))
                {
                    Globals.Instance.Log.LogMessage("DataTableManager.TableManagerWorker failed to shutdown.", LogLevel.Error);
                }
            }
            else
            {
                Globals.Instance.Log.LogMessage("DataTableManager.TableManagerWorker was already terminated.", LogLevel.Warn);
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
}
