using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using ParquetSharp;
using ParquetSharp.Arrow;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace EvolverCore.Models.DataV2
{
    public enum TickType : byte { Bid, Ask } ;
    public enum TableType : byte { Bar, Tick };

    public readonly struct Tick
    {
        public Tick(DateTime time, TickType type, double value)
        {
            Time = time;
            Type = type;
            Value = value;
        }

        public DateTime Time { get; init; }
        public TickType Type { get; init; }
        public double Value { get; init; }

        public static Schema GetSchema()
        {
            TimestampType millisecondTimestamp = new TimestampType(
                Apache.Arrow.Types.TimeUnit.Millisecond, TimeZoneInfo.Utc);

            Field[] fields = new[]
                {
                    new Field("Time", millisecondTimestamp, nullable: false),
                    new Field("Type", new Int8Type(), nullable: false),
                    new Field("Value", new DoubleType(), nullable: false)
                };

            return new Schema(fields, null);
        }

        public static RecordBatch EmptyBatch()
        {
            var schema = Tick.GetSchema();

            // Create empty builders (zero rows appended → length 0)
            var timeBuilder = new TimestampArray.Builder();   // defaults to ms, nullable=false
            var typeBuilder = new Int8Array.Builder();
            var valueBuilder = new DoubleArray.Builder();

            // Build zero-length arrays (no Append calls = empty)
            // Order must match GetSchema() fields exactly
            var arrays = new IArrowArray[]
                {
                    timeBuilder.Build(),
                    typeBuilder.Build(),
                    valueBuilder.Build()
                };

            // Wrap in RecordBatch (length must be 0)
            return new RecordBatch(schema, arrays, length: 0);
        }
    }
    public readonly struct Bar
    {
        public Bar(DateTime time, double open, double high, double low, double close, long volume)
        {
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }

        public DateTime Time { get; init; }
        public double Open { get; init; }
        public double High { get; init; }
        public double Low { get; init; }
        public double Close { get; init; }
        public long Volume { get; init; }

        public static Schema GetSchema()
        {
            TimestampType millisecondTimestamp = new TimestampType(
                Apache.Arrow.Types.TimeUnit.Millisecond, TimeZoneInfo.Utc);

            Field[] fields = new[]
                {
                    new Field("Time", millisecondTimestamp, nullable: false),
                    new Field("Open", new DoubleType(), nullable: false),
                    new Field("High", new DoubleType(), nullable: false),
                    new Field("Low", new DoubleType(), nullable: false),
                    new Field("Close", new DoubleType(), nullable: false),
                    new Field("Volume", new Int64Type(), nullable: false)
                };

            return new Schema(fields, null);
        }

        public static RecordBatch EmptyBatch()
        {
            var schema = Bar.GetSchema();

            // Create empty builders (zero rows appended → length 0)
            var timeBuilder = new TimestampArray.Builder();   // defaults to ms, nullable=false
            var openBuilder = new DoubleArray.Builder();
            var highBuilder = new DoubleArray.Builder();
            var lowBuilder = new DoubleArray.Builder();
            var closeBuilder = new DoubleArray.Builder();
            var volumeBuilder = new Int64Array.Builder();

            // Build zero-length arrays (no Append calls = empty)
            // Order must match GetSchema() fields exactly
            var arrays = new IArrowArray[]
                {
                    timeBuilder.Build(),
                    openBuilder.Build(),
                    highBuilder.Build(),
                    lowBuilder.Build(),
                    closeBuilder.Build(),
                    volumeBuilder.Build()
                };

            // Wrap in RecordBatch (length must be 0)
            return new RecordBatch(schema, arrays, length: 0);
        }
    }

    public class ColumnPointer<T> where T: struct
    {
        private Apache.Arrow.Column _column;
        private ICurrentTable _parentTable;
        private long[] _cumulativeOffsets;

        internal ColumnPointer(ICurrentTable parentTable, Apache.Arrow.Column column)
        {
            var expectedType = typeof(T) switch
            {
                Type t when t == typeof(double) => ArrowTypeId.Double,
                Type t when t == typeof(long) => ArrowTypeId.Int64,
                Type t when t == typeof(byte) => ArrowTypeId.Int8,
                Type t when t == typeof(DateTime) => ArrowTypeId.Timestamp,
                _ => throw new NotSupportedException($"Unsupported T: {typeof(T)}")
            };

            if (column.Data.DataType.TypeId != expectedType)
                throw new ArgumentException($"Column type mismatch. Expected {expectedType}, got {column.Data.DataType.TypeId}.");

            _column = column;
            _parentTable = parentTable;

            _cumulativeOffsets = new long[_column.Data.ArrayCount];

            long totalOffset = 0;
            for (int i = 0; i < _column.Data.ArrayCount; i++)
            {
                IArrowArray a = _column.Data.ArrowArray(i);
                totalOffset += a.Length;
                
                _cumulativeOffsets[i] = totalOffset;
            }

            if (totalOffset != column.Length)
                throw new InvalidOperationException("Cumulative chunk lengths do not match column length.");
        }

        public T GetValueAt(long index)
        {
            if (index < 0 || index >= _column.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            int n = System.Array.BinarySearch(_cumulativeOffsets, index + 1);
            if (n < 0) n = ~n;
            if (n >= _cumulativeOffsets.Length)
                throw new IndexOutOfRangeException("Chunk index exceeds total length.");

            IArrowArray a = _column.Data.ArrowArray(n);
            long offset = n > 0 ? _cumulativeOffsets[n - 1] : 0;
            long localIndex = index - offset;

            return typeof(T) switch
            {
                Type t when t == typeof(double) =>
                    (T)(object)(((DoubleArray)a).GetValue((int)localIndex) ?? double.NaN),

                Type t when t == typeof(long) =>
                    (T)(object)(((Int64Array)a).GetValue((int)localIndex) ?? 0),

                Type t when t == typeof(byte) =>
                    (T)(object)(((Int8Array)a).GetValue((int)localIndex) ?? throw new NullReferenceException("Enum value can never be null.")),

                Type t when t == typeof(DateTime) =>
                    (T)(object)(((TimestampArray)a).GetTimestamp((int)localIndex) ?? throw new NullReferenceException("Timestamp can never be null.")).UtcDateTime,

                _ => throw new NotSupportedException($"Unsupported T: {typeof(T)}")
            };
        }

        public T this[long barsAgo]
        {
            get
            {
                return GetValueAt(_parentTable.CurrentIndex - barsAgo);
            }
        }
    }

    public interface ICurrentTable
    {
        public int CurrentIndex { get; }
    }

    public class BarTable : ICurrentTable
    {
        public BarTable(Instrument instrument, DataInterval interval, Table table)
        {
            Instrument = instrument;
            Interval = interval;
            Table = table;
            CurrentIndex = 0;

            Time = new ColumnPointer<DateTime>(this, Table.Column(Table.Schema.GetFieldIndex("Time")));
            Open = new ColumnPointer<double>(this, Table.Column(Table.Schema.GetFieldIndex("Open")));
            Close = new ColumnPointer<double>(this, Table.Column(Table.Schema.GetFieldIndex("Close")));
            High = new ColumnPointer<double>(this, Table.Column(Table.Schema.GetFieldIndex("High")));
            Low = new ColumnPointer<double>(this, Table.Column(Table.Schema.GetFieldIndex("Low")));
            Volume = new ColumnPointer<long>(this, Table.Column(Table.Schema.GetFieldIndex("Volume")));

        }

        public Instrument Instrument { get; init; }
        public DataInterval Interval { get; init; }
        internal Table Table { get; init; }

        public int CurrentIndex { get; private set; }

        public ColumnPointer<DateTime> Time { get; private set; }
        public ColumnPointer<double> Open { get; private set; }
        public ColumnPointer<double> Close { get; private set; }
        public ColumnPointer<double> High { get; private set; }
        public ColumnPointer<double> Low { get; private set; }
        public ColumnPointer<long> Volume { get; private set; }


        public void AddColumnTest()
        {
        }
    }

    public class TickTable : ICurrentTable
    {
        public TickTable(Instrument instrument, Table table)
        {
            Instrument = instrument;
            Table = table;

            Time = new ColumnPointer<DateTime>(this, Table.Column(Table.Schema.GetFieldIndex("Time")));
            Type = new ColumnPointer<byte>(this, Table.Column(Table.Schema.GetFieldIndex("Type")));
            Value = new ColumnPointer<double>(this, Table.Column(Table.Schema.GetFieldIndex("Value")));
        }

        public Instrument Instrument { get; init; }
        public Table Table { get; init; }

        public int CurrentIndex { get; private set; }

        public ColumnPointer<DateTime> Time { get; private set; }
        public ColumnPointer<byte> Type { get; private set; }
        public ColumnPointer<double> Value { get; private set; }
    }

    public static class DataWarehouse
    {
        private static string GetBarPartitionPath(Instrument instrument, DataInterval interval, DateOnly date)
        {
            // Example: bars/AAPL/1min/2026-01-13.parquet
            string symbol = instrument.Name.Replace("/", "_"); // escape slashes if needed
            string intervalStr = interval.ToString();
            return Path.Combine(Globals.Instance.DataDirectory, "bars", symbol, intervalStr, $"{date:yyyy-MM-dd}.parquet");
        }

        private static string GetTickPartitionPath(Instrument instrument, DateOnly date)
        {
            string symbol = instrument.Name.Replace("/", "_");
            return Path.Combine(Globals.Instance.DataDirectory, "ticks", symbol, $"{date:yyyy-MM-dd}.parquet");
        }

        private static (WriterProperties parquet, ArrowWriterProperties arrow) GetWriterProperties()
        {
            WriterProperties parquetProperties = WriterProperties.GetDefaultWriterProperties();
            ArrowWriterProperties arrowProperties = ArrowWriterProperties.GetDefault();

            return (parquetProperties, arrowProperties);
        }

        private static (ReaderProperties parquet, ArrowReaderProperties arrow) GetReaderProperties()
        {
            ReaderProperties parquetProperties = ReaderProperties.GetDefaultReaderProperties();
            ArrowReaderProperties arrowProperties = ArrowReaderProperties.GetDefault();

            arrowProperties.UseThreads = true;

            return (parquetProperties, arrowProperties);
        }

        public static int GetRowGroupsCount(string filePath)
        {
            using (FileReader reader = new FileReader(filePath))
                return reader.NumRowGroups;
        }

        public static async Task<ICurrentTable> ReadToTableAsync(Instrument instrument, DataInterval interval, DateTime start, DateTime end, string[]? columnNames = null)
        {
            TableType tableType;

            switch (interval.Type)
            {
                case Interval.Tick:
                    tableType = TableType.Tick;
                    break;
                default:
                    tableType = TableType.Bar;
                    break;
            }

            List<string> filesToLoad = new List<string>();
            DateOnly date = DateOnly.FromDateTime(start);
            DateOnly endDate = DateOnly.FromDateTime(start);

            while (date <= endDate)
            {
                string partitionPath;
                switch (interval.Type)
                {
                    case Interval.Tick:
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

            Table? table = null;

            for (int i = 0; i < filesToLoad.Count; i++)
            {
                string filePath = filesToLoad[i];

                //determine which row groups to load on the specified start/end dates (for all others just load all)
                int[]? rowGroups = null;
                if (i == 0)
                {//TODO: on start date, might not want all bars...

                }
                else if (i == filesToLoad.Count - 1)
                {//TODO: on end date, might not want all bars...

                }

                Table subTable = await ReadToTableAsync(filePath, tableType, rowGroups, columnNames);
                if (table == null) table = subTable;
                else table = DataWarehouse.AppendTable(table, subTable);
            }

            if (table == null)
                throw new Exception("Unable to load data.");

            switch (tableType)
            {
                case TableType.Bar: return new BarTable(instrument, interval, table);
                case TableType.Tick: return new TickTable(instrument, table);
                default: throw new Exception($"Unknown TableType {tableType.ToString()}");
            }
        }

        private static Table AppendTable(Table table, Table subTable)
        {
            if (!table.Schema.Equals(subTable.Schema))
                throw new ArgumentException("Cannot append tables with different schemas. Schemas must match.");

            List<Apache.Arrow.Column> columns = new List<Apache.Arrow.Column>();

            for (int i = 0; i < table.ColumnCount; i++)
            {
                List<IArrowArray> arrays = new List<IArrowArray>();

                Apache.Arrow.Column c = table.Column(i);
                for (int ai = 0; ai < c.Data.ArrayCount; ai++)
                    arrays.Add(c.Data.ArrowArray(ai));

                c = subTable.Column(i);
                for (int ai = 0; ai < c.Data.ArrayCount; ai++)
                    arrays.Add(c.Data.ArrowArray(ai));

                columns.Add(new Apache.Arrow.Column(c.Field, arrays));
            }

            return new Table(table.Schema, columns);
        }


        private static async Task<Table> ReadToTableAsync(string filePath, TableType tableType, int[]? rowGroups = null, string[]? columnNames = null)
        {
            var (parquetProperties, arrowProperties) = GetReaderProperties();

            Schema schema;
            switch (tableType)
            {
                case TableType.Bar: schema = Bar.GetSchema(); break;
                case TableType.Tick: schema = Tick.GetSchema(); break;
                default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
            }

            int[]? columnIndices = null;
            if (columnNames != null)
            {
                columnIndices = columnNames.Select(name => schema.GetFieldIndex(name)).ToArray();

                if (columnIndices.Any(i => i < 0))
                {
                    var missing = columnNames.Where((name, i) => columnIndices[i] < 0).First();
                    throw new ArgumentException($"Column '{missing}' not found in schema.");
                }
            }

            using (FileReader reader = new FileReader(filePath, parquetProperties, arrowProperties))
            {
                if (rowGroups != null)
                {
                    int rowCount = reader.NumRowGroups;
                    foreach (int rowGroupIndex in rowGroups)
                    {
                        if (rowGroupIndex < 0 || rowGroupIndex >= rowCount)
                        {
                            throw new ArgumentOutOfRangeException(nameof(rowGroups),
                                $"Row group index {rowGroupIndex} is invalid (file has {rowCount} row groups).");
                        }
                    }
                }

                // Restrict reader to only the specified row groups and columns
                using (IArrowArrayStream batchReader = reader.GetRecordBatchReader(
                                        rowGroups: rowGroups,
                                        columns: columnIndices
                                        ))
                {
                    var batches = new List<RecordBatch>();

                    RecordBatch? batch;
                    while ((batch = await batchReader.ReadNextRecordBatchAsync()) != null)
                    {
                        using (batch)
                            batches.Add(batch);
                    }

                    if (batches.Count == 0)
                        switch (tableType)
                        {
                            case TableType.Bar: return Table.TableFromRecordBatches(schema, new[] { Bar.EmptyBatch() });
                            case TableType.Tick: return Table.TableFromRecordBatches(schema, new[] { Tick.EmptyBatch() });
                            default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
                        }

                    return Table.TableFromRecordBatches(schema, batches);
                }
            }
        }

        private static void WriteFromArrow(string filePath, TableType tableType, RecordBatch[] arrowBatches)
        {
            var (parquetProperties, arrowProperties) = GetWriterProperties();
            Schema schema;
            switch (tableType)
            {
                case TableType.Bar: schema = Bar.GetSchema(); break;
                case TableType.Tick: schema = Tick.GetSchema();break;
                default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
            }

            foreach (RecordBatch arrowBatch in arrowBatches)
            {
                if (!arrowBatch.Schema.Equals(schema))
                {
                    throw new EvolverException("Schema mismatch.");
                }
            }

            using (FileWriter writer = new FileWriter(filePath, schema, parquetProperties, arrowProperties))
            {
                foreach (RecordBatch arrowBatch in arrowBatches)
                {
                    writer.WriteBufferedRecordBatch(arrowBatch);
                }
                writer.Close();
            }
        }

        private static void WriteFromArrow(string filePath, TableType tableType, RecordBatch arrowBatch)
        {
            var (parquetProperties, arrowProperties) = GetWriterProperties();
            Schema schema;
            switch (tableType)
            {
                case TableType.Bar: schema = Bar.GetSchema(); break;
                case TableType.Tick: schema = Tick.GetSchema(); break;
                default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
            }

            if (!arrowBatch.Schema.Equals(schema))
            {
                throw new EvolverException("Schema mismatch.");
            }

            using (FileWriter writer = new FileWriter(filePath, schema, parquetProperties, arrowProperties))
            {
                writer.WriteRecordBatch(arrowBatch);
                writer.Close();
            }
        }

        private static void WriteFromArrow(string filePath, TableType tableType, Table arrowTable)
        {
            var (parquetProperties, arrowProperties) = GetWriterProperties();
            Schema schema;
            switch (tableType)
            {
                case TableType.Bar: schema = Bar.GetSchema(); break;
                case TableType.Tick: schema = Tick.GetSchema(); break;
                default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
            }

            if (!arrowTable.Schema.Equals(schema))
            {
                throw new EvolverException("Schema mismatch.");
            }

            using (FileWriter writer = new FileWriter(filePath, schema, parquetProperties, arrowProperties))
            {
                writer.WriteTable(arrowTable);
                writer.Close();
            }
        }
    }
}
