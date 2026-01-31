using EvolverCore.Models.DataV2;
using IronCompress;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace EvolverCore.Models
{
    internal class DataLoadJobDoneArgs
    {
        public DataLoadJobDoneArgs(DataLoadJob sourceJob, string errorMessage)
        {
            SourceJob = sourceJob;
            ErrorMessage = errorMessage;
        }

        public DataLoadJobDoneArgs(DataLoadJob sourceJob, DataTablePointer resultTable)
        {
            SourceJob = sourceJob;
            ResultTable = resultTable;
        }

        public DataLoadJob SourceJob { get; init; }
        public DataTablePointer? ResultTable { get; init; } = null;

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

        internal void FireJobDone(DataTablePointer tablePointer)
        {
            DataLoadJobDoneArgs args = new DataLoadJobDoneArgs(this, tablePointer);
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
        private Dictionary<string, Dictionary<DataInterval, DataTable>> _tables = new Dictionary<string, Dictionary<DataInterval, DataTable>>();

        private bool _disposedValue = false;
        private bool _isShutdown = false;
        private Thread _dataStoreWorker;
        private object _jobQueueLock = new object();
        private BlockingCollection<Func<CancellationToken, Task>> _dataJobQueue = new BlockingCollection<Func<CancellationToken,Task>>();
        private CancellationTokenSource _cts = new CancellationTokenSource();

        internal event EventHandler? DataWarehouseWorkerError = null;

        internal DataWarehouse(DataTableManager tableManager)
        {
            _dataStoreWorker = new Thread(dataStoreWorker);
            _dataStoreWorker.Name = "DataWarehouse Worker";
            _tableManager = tableManager;
        }

        internal async Task LoadRecords()
        {
            await _recordCollection.LoadAvailableInstrumentData();
        }

        internal void DataUpdate(ConnectionDataUpdateEventArgs args)
        {//Runs in the context of the connection data update worker
            lock(_tablesLock)
            {
                if (!_tables.ContainsKey(args.Instrument.Name)) return;

                foreach (DataInterval interval in _tables[args.Instrument.Name].Keys)
                {
                    DataTable table = _tables[args.Instrument.Name][interval];
                    if (!table.IsLive) continue;

                    table.Accumulator.AddTick(args.Time, args.Bid, args.Ask, args.Volume);
                }
            }
        }

        internal void EnqueueDataSaveJob(DataSaveJob job)
        {
            lock (_dataJobQueue)
            {
                if (_isShutdown) return;
                _dataJobQueue.Add((token) => ExecuteDataSaveJob(token, job));
            }
        }

        internal void EnqueueDataLoadJob(DataLoadJob job)
        {
            lock (_dataJobQueue)
            {
                if (_isShutdown) return;
                _dataJobQueue.Add((token) => ExecuteDataLoadJob(token, job));
            }
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
                Globals.Instance.Log.LogMessage($"Failed to save {job.Table.Interval.ToString()} data for '{job.Table.Instrument.Name}'", LogLevel.Error);
                Globals.Instance.Log.LogException(ex);
                
                job.FireJobDone(ex.Message);
                return;
            }

            job.FireJobDone();
        }

        private async Task ExecuteDataLoadJob(CancellationToken token, DataLoadJob job)
        {
            if (job.Interval.Type == IntervalSpan.Tick)
            {
                throw new NotImplementedException("Tick table data load job not yet implemented.");
            }
            else
            {
                DataTable? loadedTable = null;
                lock (_tablesLock)
                {
                    if (_tables.ContainsKey(job.Instrument.Name))
                    {
                        if (_tables[job.Instrument.Name].ContainsKey(job.Interval))
                        {
                            loadedTable = _tables[job.Instrument.Name][job.Interval];
                        }
                    }
                }

                if (loadedTable == null)
                {
                    try
                    {
                        loadedTable = await DataWarehouse.ReadToDataTableAsync(token, job.Instrument, job.Interval, job.StartTime, job.EndTime);

                        lock (_tablesLock)
                        {
                            if (_tables.ContainsKey(job.Instrument.Name))
                            {
                                if (_tables[job.Instrument.Name].ContainsKey(job.Interval))
                                    _tables[job.Instrument.Name][job.Interval] = loadedTable;
                                else
                                    _tables[job.Instrument.Name].Add(job.Interval, loadedTable);
                            }
                            else
                            {
                                _tables.Add(job.Instrument.Name, new Dictionary<DataInterval, DataTable>());
                                _tables[job.Instrument.Name].Add(job.Interval, loadedTable);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Globals.Instance.Log.LogMessage($"Failed to read {job.Interval.ToString()} data for '{job.Instrument.Name}': From={job.StartTime} To={job.EndTime}", LogLevel.Error);
                        Globals.Instance.Log.LogException(ex);
                        job.FireJobDone(ex.Message);
                        return;
                    }
                }

                DataTablePointer resultTable = new DataTablePointer(loadedTable, job.StartTime, job.EndTime);
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
                        lock (_dataJobQueue)
                        {
                            action = _dataJobQueue.Take(token);
                        }
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

        public static async Task<DataTable> ReadToDataTableAsync(CancellationToken token, Instrument instrument, DataInterval interval, DateTime start, DateTime end)
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
                case TableType.Bar: return table;
                case TableType.Tick: return table;
                default: throw new Exception($"Unknown TableType {tableType.ToString()}");
            }
        }

        private static async Task<DataTable> ReadToDataTableAsync(string filePath, TableType tableType, Instrument instrument, DataInterval interval, int[]? rowGroups = null)
        {
            ParquetSchema schema;
            switch (tableType)
            {
                case TableType.Bar: schema = Bar.GetSchema(); break;
                case TableType.Tick: schema = Tick.GetSchema(); break;
                default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
            }

            if (!File.Exists(filePath)) return new DataTable(schema, 0, instrument, interval);

            using (ParquetReader reader = await ParquetReader.CreateAsync(filePath, GetParquetProperties()))
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

                DataTable resultTable = new DataTable(schema, 0, instrument, interval);

                foreach (int rowGroupIndex in rowGroupsToRead)
                {
                    using (ParquetRowGroupReader rgReader = reader.OpenRowGroupReader(rowGroupIndex))
                    {
                        List<DataColumn> colList = new List<DataColumn>();

                        for (int i = 0; i < schema.DataFields.Length; i++)
                        {
                            DataField field = schema.DataFields[i];
                            DataColumn col = await rgReader.ReadColumnAsync(field);

                            colList.Add(col);
                        }

                        resultTable.AddColumnData(colList.ToArray());
                    }
                }

                return resultTable;
            }
        }

        internal static async Task WritePartitionedBars(DataTable table)
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
                    subTable);
            }
        }

        private static async Task WriteDataTableAsync(string filePath, TableType tableType, DataTable table)
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

            using (FileStream stream = File.Create(filePath))
            {
                using (ParquetWriter writer = await ParquetWriter.CreateAsync(schema, stream, GetParquetProperties()))
                {
                    using (ParquetRowGroupWriter rgWriter = writer.CreateRowGroup())
                    {
                        (ParquetSchema parquetSchema, DataColumn[] parquetData) = DataTableHelpers.ConvertDataTableToParquet(table);

                        foreach (DataColumn parquetColumn in parquetData)
                            await rgWriter.WriteColumnAsync(parquetColumn);

                        rgWriter.CompleteValidate();
                    }
                }
            }
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
