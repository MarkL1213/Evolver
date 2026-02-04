using NP.Utilities;
using Parquet;
using Parquet.Data;
using Parquet.File.Values.Primitives;
using Parquet.Schema;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EvolverCore.Models
{
    internal class DataLoadJobDoneArgs
    {
        public DataLoadJobDoneArgs(DataLoadJob sourceJob, string errorMessage)
        {
            SourceJob = sourceJob;
            ErrorMessage = errorMessage;
        }

        public DataLoadJobDoneArgs(DataLoadJob sourceJob, BarTable resultTable)
        {
            SourceJob = sourceJob;
            ResultTable = resultTable;
        }

        public DataLoadJob SourceJob { get; init; }
        public BarTable? ResultTable { get; init; } = null;

        public string ErrorMessage { get; init; } = string.Empty;

        public bool HasError { get { return ErrorMessage != string.Empty; } }
    }

    internal class DataLoadJob
    {
        public event EventHandler<DataLoadJobDoneArgs>? JobDone = null;

        public DataLoadJob(Instrument instrument, DataInterval interval, DateTime start, DateTime end)
        {
            Interval = interval;
            Instrument = instrument;
            StartTime = start;
            EndTime = end;
        }

        public Instrument Instrument { get; init; }
        public DataInterval Interval { get; init; }
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }


        internal void FireJobDone(string errorMessage)
        {
            DataLoadJobDoneArgs args = new DataLoadJobDoneArgs(this, errorMessage);
            JobDone?.Invoke(this, args);
        }

        internal void FireJobDone(BarTable table)
        {
            DataLoadJobDoneArgs args = new DataLoadJobDoneArgs(this, table);
            JobDone?.Invoke(this, args);
        }
    }

    internal class DataSaveJobDoneArgs
    {
        public DataSaveJobDoneArgs(DataSaveJob sourceJob, string errorMessage)
        {
            SourceJob = sourceJob;
            ErrorMessage = errorMessage;
        }

        public DataSaveJobDoneArgs(DataSaveJob sourceJob)
        {
            SourceJob = sourceJob;
        }

        public DataSaveJob SourceJob { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;

        public bool HasError { get { return ErrorMessage != string.Empty; } }
    }

    internal class DataSaveJob
    {
        public event EventHandler<DataSaveJobDoneArgs>? JobDone = null;
        public DataSaveJob(BarTable table) { Table = table; }

        public BarTable Table { get; init; }

        internal void FireJobDone(string errorMessage)
        {
            JobDone?.Invoke(this, new DataSaveJobDoneArgs(this, errorMessage));
        }

        internal void FireJobDone()
        {
            JobDone?.Invoke(this, new DataSaveJobDoneArgs(this));
        }
    }

    public class DataWarehouse : IDisposable
    {
        DataTableManager _tableManager;
        private DataTableRecordCollection _recordCollection = new DataTableRecordCollection();

        private object _tablesLock = new object();
        private Dictionary<string, Dictionary<DataInterval, List<BarTable>>> _barTables = new Dictionary<string, Dictionary<DataInterval, List<BarTable>>>();

        private bool _disposedValue = false;
        private bool _isShutdown = false;
        private Thread _dataStoreWorker;
        private BlockingCollection<Func<CancellationToken, Task>> _dataJobQueue = new BlockingCollection<Func<CancellationToken, Task>>();
        private CancellationTokenSource _cts = new CancellationTokenSource();

        internal event EventHandler? DataWarehouseWorkerError = null;

        internal DataWarehouse(DataTableManager tableManager)
        {
            _dataStoreWorker = new Thread(dataStoreWorker);
            _dataStoreWorker.Name = "DataWarehouse Worker";
            _dataStoreWorker.Start();
            _tableManager = tableManager;
        }

        internal async Task LoadRecords()
        {
            await _recordCollection.LoadAvailableInstrumentData();
        }

        internal void DataUpdate(ConnectionDataUpdateEventArgs args)
        {//Runs in the context of the connection data update worker
            lock (_tablesLock)
            {
                if (!_barTables.ContainsKey(args.Instrument.Name)) return;

                foreach (DataInterval interval in _barTables[args.Instrument.Name].Keys)
                {
                    List<BarTable> tables = _barTables[args.Instrument.Name][interval];
                    foreach (BarTable table in tables)
                    {
                        if (!table.IsLive) continue;

                        table.AddTick(args.Time, args.Bid, args.Ask, args.Volume);
                    }
                }
            }
        }

        internal void EnqueueDataSaveJob(DataSaveJob job)
        {

            if (_isShutdown) return;
            _dataJobQueue.Add((token) => ExecuteDataSaveJob(token, job));
        }

        internal void EnqueueDataLoadJob(DataLoadJob job)
        {

            if (_isShutdown) return;
            _dataJobQueue.Add((token) => ExecuteDataLoadJob(token, job));
        }

        private async Task ExecuteDataSaveJob(CancellationToken token, DataSaveJob job)
        {
            try
            {
                DataTable subData = job.Table.Table!.DynamicSlice();
                await DataWarehouse.WritePartitionedBars(subData);
            }
            catch (Exception ex)
            {
                Globals.Instance.Log.LogMessage($"Failed to save {job.Table.Interval} data for '{job.Table.Instrument!.Name}'", LogLevel.Error);
                Globals.Instance.Log.LogException(ex);

                job.FireJobDone(ex.Message);
                return;
            }

            job.FireJobDone();
        }



        private async Task<BarTable> DownloadDataChunk(Instrument instrument, DataInterval interval, DateTime start, DateTime end, CancellationToken token)
        {
            try
            {
                //FIXME : implement download
                throw new NotImplementedException("DownloadDataChunk");
            }
            catch (Exception ex)
            {
                Globals.Instance.Log.LogMessage($"Failed to download {interval.ToString()} data for '{instrument.Name}': From={start} To={end}", LogLevel.Error);
                Globals.Instance.Log.LogException(ex);

                throw new EvolverException("Data load failure.", ex);
            }
        }

        private (bool eventFired, List<(DateTime start, DateTime end, BarTable table)> cacheCoverages) determineCacheCovereage(DataLoadJob job)
        {
            List<(DateTime start, DateTime end, BarTable table)> cacheCoverages = new List<(DateTime start, DateTime end, BarTable table)>();

            lock (_tablesLock)
            {
                if (_barTables.ContainsKey(job.Instrument.Name) && _barTables[job.Instrument.Name].ContainsKey(job.Interval))
                {
                    foreach (BarTable table in _barTables[job.Instrument.Name][job.Interval])
                    {
                        if (job.StartTime >= table.MinTime && job.EndTime <= table.MaxTime)
                        {
                            job.FireJobDone(table);
                            return (true, cacheCoverages.OrderBy(c=>c.start).ToList());
                        }

                        bool overlapStart = job.StartTime > table.MinTime;
                        bool overlapEnd = job.EndTime < table.MaxTime;

                        if (overlapStart || overlapEnd)
                        {
                            cacheCoverages.Add((table.MinTime, table.MaxTime, table));
                        }
                    }
                }
            }

            return (false, cacheCoverages.OrderBy(c => c.start).ToList());
        }

        private List<(DateTime start, DateTime end)> determineMissingPeriods(DataLoadJob job, List<(DateTime start, DateTime end, BarTable table)> cacheCoverages)
        {
            List<(DateTime start, DateTime end)> missingPeriods = new List<(DateTime start, DateTime end)>();
            if (cacheCoverages.Count == 0) missingPeriods.Add((job.StartTime, job.EndTime));
            else
            {
                // Initial gap (before first cached piece)
                (DateTime start, DateTime end, BarTable table) first = cacheCoverages[0];
                DateTime initialGapEnd = job.Interval.Add(first.start, -1);
                if (job.StartTime > first.start)
                    missingPeriods.Add((job.StartTime, initialGapEnd));

                DateTime currentEnd = first.end;
                for (int i = 1; i < cacheCoverages.Count; i++)
                {
                    var cov = cacheCoverages[i];
                    DateTime gapStart = job.Interval.Add(currentEnd, 1);
                    DateTime gapEnd = job.Interval.Add(cov.start, -1);

                    if (gapEnd < job.EndTime)
                        missingPeriods.Add((gapStart, gapEnd));

                    currentEnd = cov.end;
                }

                // Final gap (after last cached piece)
                DateTime finalGapStart = job.Interval.Add(currentEnd, 1);
                if (finalGapStart < job.EndTime)
                    missingPeriods.Add((finalGapStart, job.EndTime));
            }
            return missingPeriods.OrderBy(t=>t.start).ToList();
        }


        private async Task<List<(DateTime start, DateTime end, BarTable dt)>> buildLoadJobChunks(DataLoadJob job,
            List<(DateTime start, DateTime end)> missingPeriods,
            List<(DateTime start, DateTime end)> availableDatesOnDisk,
            CancellationToken token
            )
        {
            List<(DateTime start, DateTime end, BarTable dt)> chunks = new List<(DateTime start, DateTime end, BarTable dt)> ();

            int mpIdx=0;
            int adodIdx = 0;

            while (true)
            {
                if (mpIdx < missingPeriods.Count && missingPeriods[mpIdx].start < (adodIdx < availableDatesOnDisk.Count ? availableDatesOnDisk[adodIdx].start : DateTime.MaxValue))
                {
                    BarTable dt = await DownloadDataChunk(job.Instrument, job.Interval, missingPeriods[mpIdx].start, missingPeriods[mpIdx].end, token);
                    chunks.Add((missingPeriods[mpIdx].start, missingPeriods[mpIdx].end, dt));
                    mpIdx++;
                }
                else if (adodIdx < availableDatesOnDisk.Count)
                {
                    BarTable dt;
                    bool loadFailed = false;
                    try
                    {
                        dt = await DataWarehouse.ReadToDataTableAsync(token, job.Instrument, job.Interval, availableDatesOnDisk[adodIdx].start, availableDatesOnDisk[adodIdx].end);
                        chunks.Add((availableDatesOnDisk[adodIdx].start, availableDatesOnDisk[adodIdx].end, dt));
                    }
                    catch
                    {
                        loadFailed = true;
                    }

                    if (loadFailed)
                    {
                        dt = await DownloadDataChunk(job.Instrument, job.Interval, availableDatesOnDisk[adodIdx].start, availableDatesOnDisk[adodIdx].end, token);
                        chunks.Add((availableDatesOnDisk[adodIdx].start, availableDatesOnDisk[adodIdx].end, dt));
                    }
                    
                    adodIdx++;
                }

                if (mpIdx >= missingPeriods.Count && adodIdx >= availableDatesOnDisk.Count)
                    break;
            }

            return chunks.OrderBy(t => t.start).ToList();
        }


        private async Task ExecuteDataLoadJob(CancellationToken token, DataLoadJob job)
        {
            if (job.Interval.Type == IntervalSpan.Tick)
            {
                throw new NotImplementedException("Tick table data load job not yet implemented.");
            }
            else
            {
                (bool eventFired, List<(DateTime start, DateTime end, BarTable table)> cacheCoverages) =
                    determineCacheCovereage(job);
                if (eventFired) return;

                DataAvailability availability = _recordCollection.IsDataAvailable(job.Instrument, job.Interval, job.StartTime, job.EndTime);
                List<(DateTime start, DateTime end)> missingPeriods = determineMissingPeriods(job, cacheCoverages);

                List<(DateTime start, DateTime end, BarTable table)> chunks = await buildLoadJobChunks(job, missingPeriods, availability.AvailableDates, token);


                DataTable mergeTable = new DataTable(Bar.GetSchema(), 0, TableType.Bar, job.Instrument, job.Interval);

                int ccIdx = 0;
                int chunkIdx = 0;

                while (true)
                {
                    if (ccIdx < cacheCoverages.Count && cacheCoverages[ccIdx].start < (chunkIdx < chunks.Count ? chunks[chunkIdx].start : DateTime.MaxValue))
                    {
                        mergeTable.AppendTable(cacheCoverages[ccIdx].table.Table);
                        ccIdx++;
                    }
                    else if (chunkIdx < chunks.Count)
                    {
                        mergeTable.AppendTable(chunks[chunkIdx].table.Table);
                        chunkIdx++;
                    }

                    if (ccIdx >= cacheCoverages.Count && chunkIdx >= chunks.Count)
                        break;
                }

                BarTable resultTable = new BarTable(mergeTable);

                lock (_tablesLock)
                {
                    if (!_barTables.ContainsKey(job.Instrument.Name))
                        _barTables.Add(job.Instrument.Name, new Dictionary<DataInterval, List<BarTable>>());

                    if (!_barTables[job.Instrument.Name].ContainsKey(job.Interval))
                        _barTables[job.Instrument.Name].Add(job.Interval, new List<BarTable>());

                    _barTables[job.Instrument.Name][job.Interval].Add(resultTable);

                    if (_barTables.ContainsKey(job.Instrument.Name) && _barTables[job.Instrument.Name].ContainsKey(job.Interval))
                    {
                        List<BarTable> cacheList = _barTables[job.Instrument.Name][job.Interval];
                        foreach ((DateTime start, DateTime end, BarTable table) cacheEntry in cacheCoverages)
                        {
                            List<BarTablePointer> pointers = cacheEntry.table.RegisteredPointers.ToList();

                            foreach (BarTablePointer pointer in pointers)
                                pointer.SetTable(resultTable, pointer.Time.GetValueAt(0), pointer.Time.GetValueAt(pointer.Time.Count - 1));

                            cacheList.Remove(cacheEntry.table);
                        }
                    }
                }

                job.FireJobDone(resultTable);
            }
        }

        private void dataStoreWorker()
        {
            CancellationToken token = _cts.Token;

            try
            {
                while (true)
                {
                    Func<CancellationToken, Task> action;
                    try
                    {
                        action = _dataJobQueue.Take(token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (InvalidOperationException) { break; }

                    try
                    {
                        Task task = action(token);
                        task.Wait(token);
                    }
                    catch (Exception e)
                    {
                        Globals.Instance.Log.LogMessage("DataWarehouse Worker event exception:", LogLevel.Error);
                        Globals.Instance.Log.LogException(e);
                    }
                }
            }
            catch (Exception e)
            {
                Globals.Instance.Log.LogMessage("DataWarehouse Worker thread exception:", LogLevel.Error);
                Globals.Instance.Log.LogException(e);

                DataWarehouseWorkerError?.Invoke(this, EventArgs.Empty);
            }
        }

        internal void Shutdown()
        {
            lock (_dataJobQueue)
            {
                _isShutdown = true;
                _dataJobQueue.CompleteAdding();
            }

            if (_dataStoreWorker.IsAlive)
            {
                _cts.Cancel(); // Trigger cancellation
                _dataStoreWorker.Join();//wait forever, do NOT interrupt potential disk i/o
            }
            else
            {
                Globals.Instance.Log.LogMessage("DataWarehouse Worker was already terminated.", LogLevel.Warn);
            }
        }


        internal BarTablePointer CreateTablePointer(Instrument instrument, DataInterval interval, DateTime start, DateTime end, bool isLive = false)
        {
            BarTablePointer btp = new BarTablePointer(null, instrument, interval);
            DataLoadJob job = new DataLoadJob(instrument, interval, start, end);
            BarTable? bt = null;

            lock (_tablesLock)
            {
                if (_barTables.ContainsKey(instrument.Name) && _barTables[instrument.Name].ContainsKey(interval))
                {
                    foreach (BarTable table in _barTables[instrument.Name][interval])
                    {
                        if (table.MinTime >= start && table.MaxTime <= end)
                            bt = table;
                    }
                }
            }

            if (bt != null)
            {
                btp.OnDataTableLoaded(this, new DataLoadJobDoneArgs(job, bt));
                return btp;
            }

            job.JobDone += btp.OnDataTableLoaded;
            EnqueueDataLoadJob(job);
            return btp;
        }


        #region static data readers/writers
        internal static string GetBarPartitionPath(Instrument instrument, DataInterval interval, DateOnly date)
        {
            // Example: bars/AAPL/1min/2026-01-13.parquet
            string symbol = instrument.Name.Replace("/", "_"); // escape slashes if needed
            string intervalStr = interval.ToString();
            return Path.Combine(Globals.Instance.DataDirectory, "bars", symbol, intervalStr, $"{date:yyyy-MM-dd}.parquet");
        }

        internal static string GetTickPartitionPath(Instrument instrument, DateOnly date)
        {
            string symbol = instrument.Name.Replace("/", "_");
            return Path.Combine(Globals.Instance.DataDirectory, "ticks", symbol, $"{date:yyyy-MM-dd}.parquet");
        }

        private static ParquetOptions GetParquetProperties()
        {
            ParquetOptions options = new ParquetOptions();
            return options;
        }

        public static async Task<int> GetRowGroupsCount(string filePath)
        {
            using (ParquetReader reader = await ParquetReader.CreateAsync(filePath))
                return reader.RowGroupCount;
        }

        public static async Task<BarTable> ReadToDataTableAsync(CancellationToken token, Instrument instrument, DataInterval interval, DateTime start, DateTime end)
        {
            TableType tableType;

            switch (interval.Type)
            {
                case IntervalSpan.Tick:
                    tableType = TableType.Tick;
                    break;
                default:
                    tableType = TableType.Bar;
                    break;
            }

            List<string> filesToLoad = new List<string>();
            DateOnly date = DateOnly.FromDateTime(start);
            DateOnly endDate = DateOnly.FromDateTime(end);

            while (date <= endDate)
            {
                string partitionPath;
                switch (interval.Type)
                {
                    case IntervalSpan.Tick:
                        partitionPath = GetTickPartitionPath(instrument, date);
                        break;
                    default:
                        partitionPath = GetBarPartitionPath(instrument, interval, date);
                        break;
                }
                if (!File.Exists(partitionPath))
                    throw new Exception($"Missing data file for intrument={instrument.Name} interval={interval.ToString()} date={date.ToString()}");

                filesToLoad.Add(partitionPath);
                date = date.AddDays(1);
            }
            if (filesToLoad.Count == 0)
                throw new Exception($"No data files found for intrument={instrument.Name} interval={interval.ToString()} start={start.ToString()} end={end.ToString()}");

            DataTable? table = null;

            for (int i = 0; i < filesToLoad.Count; i++)
            {
                string filePath = filesToLoad[i];

                DataTable subTable = await ReadToDataTableAsync(filePath, tableType, instrument, interval, null);

                if (table == null) table = subTable;
                else table.AppendTable(subTable);
            }

            if (table == null)
                throw new Exception("Unable to load data.");

            switch (tableType)
            {
                case TableType.Bar: return new BarTable(table);
                //case TableType.Tick: return table;
                default: throw new Exception($"Unknown TableType {tableType.ToString()}");
            }
        }

        private static async Task<DataTable> ReadToDataTableAsync(string filePath, TableType tableType, Instrument instrument, DataInterval interval, int[]? rowGroups = null, CancellationToken cancelToken = default)
        {
            ParquetSchema schema;
            switch (tableType)
            {
                case TableType.Bar: schema = Bar.GetSchema(); break;
                case TableType.Tick: schema = Tick.GetSchema(); break;
                default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
            }

            if (!File.Exists(filePath)) return new DataTable(schema, 0, tableType, instrument, interval);

            using (ParquetReader reader = await ParquetReader.CreateAsync(filePath, GetParquetProperties(), cancelToken))
            {
                int[] rowGroupsToRead;
                if (rowGroups != null)
                {
                    rowGroupsToRead = new int[rowGroups.Length];
                    int rowCount = reader.RowGroupCount;
                    for (int i = 0; i < rowGroups.Length; i++)
                    {
                        int rowGroupIndex = rowGroups[i];
                        rowGroupsToRead[i] = rowGroupIndex;
                        if (rowGroupIndex < 0 || rowGroupIndex >= rowCount)
                        {
                            throw new ArgumentOutOfRangeException(nameof(rowGroups),
                                $"Row group index {rowGroupIndex} is invalid (file has {rowCount} row groups).");
                        }
                    }
                }
                else
                {
                    rowGroupsToRead = new int[reader.RowGroupCount];
                    for (int i = 0; i < reader.RowGroupCount; i++)
                        rowGroupsToRead[i] = i;
                }

                DataTable resultTable = new DataTable(schema, 0, tableType, instrument, interval);

                foreach (int rowGroupIndex in rowGroupsToRead)
                {
                    using (ParquetRowGroupReader rgReader = reader.OpenRowGroupReader(rowGroupIndex))
                    {
                        List<DataColumn> colList = new List<DataColumn>();

                        for (int i = 0; i < schema.DataFields.Length; i++)
                        {
                            DataField field = schema.DataFields[i];
                            DataColumn col = await rgReader.ReadColumnAsync(field, cancelToken);

                            colList.Add(col);
                        }

                        resultTable.AddColumnData(colList.ToArray());
                    }
                }

                return resultTable;
            }
        }

        internal static async Task WritePartitionedBars(DataTable table, CancellationToken cancelToken = default)
        {
            if (table.RowCount == 0) return; // nothing to write

            if (!SchemaHelpers.ValueEqual(table.Schema, Bar.GetSchema()))
                throw new ArgumentException("Schema mismatch.");

            DataTableColumn<DateTime>? timeColumn = table.Column("Time") as DataTableColumn<DateTime>;
            if (timeColumn == null) throw new ArgumentException("WritePartitionedBars: unable to write table with no Time column.");


            // Group (globalIndex, sliceLength) by DateOnly
            Dictionary<DateOnly, (int GlobalIndex, int Length)> groups = new Dictionary<DateOnly, (int, int)>();
            int globalIndex = 0;


            for (int j = 0; j < table.RowCount; j++)
            {
                DateTime ts = ((DateTime?)timeColumn.GetValueAt(j)) ?? throw new EvolverException("Null Time value.");
                DateOnly date = DateOnly.FromDateTime(ts);

                if (!groups.TryGetValue(date, out var group))
                {
                    group = (globalIndex, 1);
                    groups.Add(date, group);
                }
                else
                    groups[date] = (group.GlobalIndex, group.Length + 1);

                globalIndex++;
            }

            // For each date group, create sub-Table and write
            foreach (KeyValuePair<DateOnly, (int, int)> kvp in groups)
            {
                DateOnly date = kvp.Key;
                (int Index, int Length) group = kvp.Value;

                if (group.Length == 0) continue;

                DataTable subTable = table.Slice(group.Index, group.Length);

                await WriteDataTableAsync(
                    GetBarPartitionPath(table.Instrument, table.Interval, date),
                    TableType.Bar,
                    subTable, cancelToken);
            }
        }

        private static async Task WriteDataTableAsync(string filePath, TableType tableType, DataTable table, CancellationToken cancelToken = default)
        {
            ParquetSchema schema;
            switch (tableType)
            {
                case TableType.Bar: schema = Bar.GetSchema(); break;
                case TableType.Tick: schema = Tick.GetSchema(); break;
                default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
            }

            if (!SchemaHelpers.ValueEqual(table.Schema, schema))
            {
                throw new EvolverException("Schema mismatch.");
            }

            string? dirName = Path.GetDirectoryName(filePath);
            if (dirName == null)
                throw new EvolverException();

            Directory.CreateDirectory(dirName);
            //if (!File.Exists(filePath)) ;

            string tempPath = filePath + ".tmp";

            using (FileStream stream = File.Create(tempPath))
            {
                using (ParquetWriter writer = await ParquetWriter.CreateAsync(schema, stream, GetParquetProperties(), false, cancelToken))
                {
                    using (ParquetRowGroupWriter rgWriter = writer.CreateRowGroup())
                    {
                        (ParquetSchema parquetSchema, DataColumn[] parquetData) = DataTableHelpers.ConvertDataTableToParquet(table);

                        foreach (DataColumn parquetColumn in parquetData)
                            await rgWriter.WriteColumnAsync(parquetColumn, cancelToken);

                        rgWriter.CompleteValidate();
                    }
                }
            }

            File.Move(tempPath, filePath, overwrite: true);
        }
        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Shutdown();
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
