using Apache.Arrow;
using Apache.Arrow.Types;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace EvolverCore.Models.DataV2
{
    public enum TickType : byte { Bid, Ask };
    
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

            Apache.Arrow.Field[] fields = new[]
                {
                    new Apache.Arrow.Field("Time", millisecondTimestamp, nullable: false),
                    new Apache.Arrow.Field("Type", new UInt8Type(), nullable: false),
                    new Apache.Arrow.Field("Value", new DoubleType(), nullable: false)
                };

            return new Schema(fields, null);
        }

        public static RecordBatch EmptyBatch()
        {
            var schema = Tick.GetSchema();

            // Create empty builders (zero rows appended → length 0)
            var timeBuilder = new TimestampArray.Builder();   // defaults to ms, nullable=false
            var typeBuilder = new UInt8Array.Builder();
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

            Apache.Arrow.Field[] fields = new[]
                {
                    new Apache.Arrow.Field("Time", millisecondTimestamp, nullable: false),
                    new Apache.Arrow.Field("Open", new DoubleType(), nullable: false),
                    new Apache.Arrow.Field("High", new DoubleType(), nullable: false),
                    new Apache.Arrow.Field("Low", new DoubleType(), nullable: false),
                    new Apache.Arrow.Field("Close", new DoubleType(), nullable: false),
                    new Apache.Arrow.Field("Volume", new Int64Type(), nullable: false)
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

    public class ColumnPointer<T> where T : struct
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
                Type t when t == typeof(byte) => ArrowTypeId.UInt8,
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
                    (T)(object)(((UInt8Array)a).GetValue((int)localIndex) ?? throw new NullReferenceException("Enum value can never be null.")),

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
            DateOnly endDate = DateOnly.FromDateTime(end);

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

        private static Table AppendTable(Table table, Table? subTable)
        {
            if (subTable != null && !SchemaHelpers.ValueEqual(table.Schema, subTable.Schema))
                throw new ArgumentException("Cannot append tables with different schemas. Schemas must match.");

            List<Apache.Arrow.Column> columns = new List<Apache.Arrow.Column>();

            for (int i = 0; i < table.ColumnCount; i++)
            {
                List<IArrowArray> arrays = new List<IArrowArray>();

                Apache.Arrow.Column c = table.Column(i);
                for (int ai = 0; ai < c.Data.ArrayCount; ai++)
                    arrays.Add(c.Data.ArrowArray(ai));

                if (subTable != null)
                {
                    c = subTable.Column(i);
                    for (int ai = 0; ai < c.Data.ArrayCount; ai++)
                        arrays.Add(c.Data.ArrowArray(ai));
                }

                columns.Add(new Apache.Arrow.Column(c.Field, arrays));
            }

            return new Table(table.Schema, columns);
        }


        private static async Task<Table> ReadToTableAsync(string filePath, TableType tableType, int[]? rowGroups = null, string[]? columnNames = null)
        {
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

            if (!File.Exists(filePath))
            {
                switch (tableType)
                {
                    case TableType.Bar:
                        RecordBatch emptyBarBatch = Bar.EmptyBatch();
                        return Table.TableFromRecordBatches(emptyBarBatch.Schema, new[] { emptyBarBatch });
                    case TableType.Tick:
                        RecordBatch emptyTickBatch = Tick.EmptyBatch();
                        return Table.TableFromRecordBatches(emptyTickBatch.Schema, new[] { emptyTickBatch });
                    default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
                }
            }

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

                var batches = new List<RecordBatch>();
                foreach (int rowGroupIndex in rowGroupsToRead)
                {
                    DataColumn[] columns = await reader.ReadEntireRowGroupAsync(rowGroupIndex);
                    RecordBatch batch = DataTableHelpers.ConvertParquetToArrowBatch(columns, reader.Schema);
                    batches.Add(batch);
                }

                if (batches.Count == 0)
                    switch (tableType)
                    {
                        case TableType.Bar:
                            RecordBatch emptyBarBatch = Bar.EmptyBatch();
                            return Table.TableFromRecordBatches(emptyBarBatch.Schema, new[] { emptyBarBatch });
                        case TableType.Tick:
                            RecordBatch emptyTickBatch = Tick.EmptyBatch();
                            return Table.TableFromRecordBatches(emptyTickBatch.Schema, new[] { emptyTickBatch });
                        default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
                    }

                return Table.TableFromRecordBatches(batches[0].Schema, batches);
            }
        }

        private static async Task WriteFromArrowAsync(string filePath, TableType tableType, RecordBatch[] arrowBatches)
        {
            Schema schema;
            switch (tableType)
            {
                case TableType.Bar: schema = Bar.GetSchema(); break;
                case TableType.Tick: schema = Tick.GetSchema(); break;
                default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
            }

            foreach (RecordBatch arrowBatch in arrowBatches)
            {
                if (!SchemaHelpers.ValueEqual(arrowBatch.Schema, schema))
                {
                    throw new EvolverException("Schema mismatch.");
                }
            }

            string? dirName = Path.GetDirectoryName(filePath);
            if (dirName == null)
                throw new EvolverException();

            Directory.CreateDirectory(dirName);
            if (!File.Exists(filePath)) File.Create(filePath);

            ParquetSchema pSchema = SchemaHelpers.ConvertArrowToParquet(schema);

            using (FileStream stream = new FileStream(filePath, FileMode.Truncate))
            {
                using (ParquetWriter writer = await ParquetWriter.CreateAsync(pSchema, stream, GetParquetProperties()))
                {
                    using (ParquetRowGroupWriter rgWriter = writer.CreateRowGroup())
                    {
                        foreach (RecordBatch arrowBatch in arrowBatches)
                        {
                            (ParquetSchema parquetSchema, DataColumn[] parquetData) = DataTableHelpers.ConvertArrowBatchToParquet(arrowBatch);

                            foreach (DataColumn parquetColumn in parquetData)
                                await rgWriter.WriteColumnAsync(parquetColumn);
                        }

                        rgWriter.CompleteValidate();
                    }
                }
            }
        }

        private static async Task WriteFromArrowAsync(string filePath, TableType tableType, RecordBatch arrowBatch)
        {
            Schema schema;
            switch (tableType)
            {
                case TableType.Bar: schema = Bar.GetSchema(); break;
                case TableType.Tick: schema = Tick.GetSchema(); break;
                default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
            }

            if (!SchemaHelpers.ValueEqual(arrowBatch.Schema, schema))
            {
                throw new EvolverException("Schema mismatch.");
            }

            string? dirName = Path.GetDirectoryName(filePath);
            if (dirName == null)
                throw new EvolverException();

            Directory.CreateDirectory(dirName);
            if (!File.Exists(filePath)) File.Create(filePath);

            ParquetSchema pSchema = SchemaHelpers.ConvertArrowToParquet(schema);

            using (FileStream stream = new FileStream(filePath, FileMode.Truncate))
            {
                using (ParquetWriter writer = await ParquetWriter.CreateAsync(pSchema, stream, GetParquetProperties()))
                {
                    using (ParquetRowGroupWriter rgWriter = writer.CreateRowGroup())
                    {
                        (ParquetSchema parquetSchema, DataColumn[] parquetData) = DataTableHelpers.ConvertArrowBatchToParquet(arrowBatch);

                        foreach (DataColumn parquetColumn in parquetData)
                            await rgWriter.WriteColumnAsync(parquetColumn);

                        rgWriter.CompleteValidate();
                    }
                }
            }
        }

        // New partitioned write method
        internal static async Task WritePartitionedBars(Table arrowTable, Instrument instrument, DataInterval interval)
        {
            if (arrowTable.RowCount == 0) return; // nothing to write

            if (!SchemaHelpers.ValueEqual(arrowTable.Schema, Bar.GetSchema()))
                throw new EvolverException("Schema mismatch.");

            ChunkedArray timeColumn = arrowTable.Column(arrowTable.Schema.GetFieldIndex("Time")).Data;

            // Group (globalIndex, sliceLength) by DateOnly
            Dictionary<DateOnly, (int GlobalIndex, int Length)> groups = new Dictionary<DateOnly, (int, int)>();
            int globalIndex = 0;

            for (int i = 0; i < timeColumn.ArrayCount; i++)
            {
                IArrowArray a = timeColumn.ArrowArray(i);
                TimestampArray? tsArray = a as TimestampArray;
                if (tsArray == null)
                    throw new EvolverException();

                for (int j = 0; j < tsArray.Length; j++)
                {
                    DateTimeOffset ts = tsArray.GetTimestamp(j) ?? throw new EvolverException("Null Time value.");
                    DateOnly date = DateOnly.FromDateTime(ts.UtcDateTime);

                    if (!groups.TryGetValue(date, out var group))
                    {
                        group = (globalIndex, 1);
                        groups.Add(date, group);
                    }
                    else
                        groups[date] = (group.GlobalIndex, group.Length + 1);

                    globalIndex++;
                }
            }

            // For each date group, create sub-Table and write
            foreach (KeyValuePair<DateOnly, (int, int)> kvp in groups)
            {
                DateOnly date = kvp.Key;
                (int Index, int Length) group = kvp.Value;

                if (group.Length == 0) continue;

                // Create sub-Table by slicing each column at the group's globalIndex and sliceLength
                List<Apache.Arrow.Column> subColumns = new List<Apache.Arrow.Column>();
                for (int colIdx = 0; colIdx < arrowTable.Schema.FieldsList.Count; colIdx++)
                    subColumns.Add(arrowTable.Column(colIdx).Slice(group.Index, group.Length));

                Table subTable = new Table(arrowTable.Schema, subColumns);

                ////////
                //TEST CODE - remove after verification
                BarTable barTable = new BarTable(instrument, interval, subTable);
                Apache.Arrow.Column c = barTable.Table.Column(barTable.Table.Schema.GetFieldIndex("Time"));
                ColumnPointer<DateTime> cp = new ColumnPointer<DateTime>(barTable, c);
                DateTime fileStartDate = cp.GetValueAt(0);
                DateTime fileEndDate = cp.GetValueAt(c.Length - 1);
                Globals.Instance.Log.LogMessage($"subTable {date.ToString()}: Start={fileStartDate} End={fileEndDate}", LogLevel.Info);
                ////////

                await WriteFromArrowAsync(
                    GetBarPartitionPath(instrument, interval, date),
                    TableType.Bar,
                    subTable);
            }
        }

        private static async Task WriteFromArrowAsync(string filePath, TableType tableType, Table arrowTable)
        {
            Schema schema;
            switch (tableType)
            {
                case TableType.Bar: schema = Bar.GetSchema(); break;
                case TableType.Tick: schema = Tick.GetSchema(); break;
                default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
            }

            if (!SchemaHelpers.ValueEqual(arrowTable.Schema, schema))
            {
                throw new EvolverException("Schema mismatch.");
            }

            string? dirName = Path.GetDirectoryName(filePath);
            if (dirName == null)
                throw new EvolverException();

            Directory.CreateDirectory(dirName);
            if (!File.Exists(filePath)) File.Create(filePath);

            ParquetSchema pSchema = SchemaHelpers.ConvertArrowToParquet(schema);

            using (FileStream stream = new FileStream(filePath, FileMode.Truncate))
            {
                using (ParquetWriter writer = await ParquetWriter.CreateAsync(pSchema, stream, GetParquetProperties()))
                {
                    using (ParquetRowGroupWriter rgWriter = writer.CreateRowGroup())
                    {
                        (ParquetSchema parquetSchema, DataColumn[] parquetData) = DataTableHelpers.ConvertArrowToParquet(arrowTable);

                        foreach (DataColumn parquetColumn in parquetData)
                            await rgWriter.WriteColumnAsync(parquetColumn);

                        rgWriter.CompleteValidate();
                    }
                }
            }
        }


        //private static Table CloneTable(Table original)
        //{
        //    List<Apache.Arrow.Column> clonedColumns = new List<Apache.Arrow.Column>(original.Schema.FieldsList.Count);

        //    for (int i = 0; i < original.Schema.FieldsList.Count; i++)
        //    {
        //        var col = original.Column(i);
        //        ChunkedArray chunked = col.Data;

        //        List<IArrowArray> clonedArrays = new List<IArrowArray>(chunked.ArrayCount);

        //        for (int chunkIdx = 0; chunkIdx < chunked.ArrayCount; chunkIdx++)
        //            clonedArrays.Add(CloneArray(chunked.ArrowArray(chunkIdx)));

        //        clonedColumns.Add(new Apache.Arrow.Column(col.Field, clonedArrays));
        //    }

        //    return new Table(original.Schema, clonedColumns);
        //}

        //private static IArrowArray CloneArray(IArrowArray original)
        //{
        //    if (original is DoubleArray da)
        //    {
        //        var builder = new DoubleArray.Builder();
        //        builder.Reserve((int)original.Length);
        //        for (int i = 0; i < original.Length; i++)
        //        {
        //            double? v = da.GetValue(i);
        //            if (v.HasValue)
        //                builder.Append(v.Value);
        //            else
        //                builder.AppendNull();
        //        }
        //        return builder.Build();
        //    }
        //    if (original is Int64Array ia)
        //    {
        //        var builder = new Int64Array.Builder();
        //        builder.Reserve((int)original.Length);
        //        for (int i = 0; i < original.Length; i++)
        //        {
        //            long? v = ia.GetValue(i);
        //            if (v.HasValue)
        //                builder.Append(v.Value);
        //            else
        //                builder.AppendNull();
        //        }
        //        return builder.Build();
        //    }
        //    if (original is TimestampArray ta)
        //    {
        //        var builder = new TimestampArray.Builder();
        //        builder.Reserve((int)original.Length);
        //        for (int i = 0; i < original.Length; i++)
        //        {
        //            DateTimeOffset? v = ta.GetTimestamp(i);
        //            if (v.HasValue)
        //                builder.Append(v.Value);
        //            else
        //                builder.AppendNull();
        //        }
        //        return builder.Build();
        //    }
        //    if (original is UInt8Array ba)
        //    {
        //        var builder = new UInt8Array.Builder();
        //        builder.Reserve((int)original.Length);
        //        for (int i = 0; i < original.Length; i++)
        //        {
        //            byte? v = ba.GetValue(i);
        //            if (v.HasValue)
        //                builder.Append(v.Value);
        //            else
        //                builder.AppendNull();
        //        }
        //        return builder.Build();
        //    }

        //    throw new NotSupportedException($"No clone support for {original.GetType().Name}");
        //}

    }

    internal static class SchemaHelpers
    {
        internal static ParquetSchema ConvertArrowToParquet(Schema aSchema)
        {
            List<DataField> fields = new List<DataField>();

            foreach (var field in aSchema.FieldsList)
            {
                var clrType = DataTableHelpers.ConvertArrowTypeToType(field.DataType);
                var pField = new DataField(field.Name, clrType, field.IsNullable);
                fields.Add(pField);
            }

            return new ParquetSchema(fields);
        }

        internal static Schema ConvertParquetToArrow(ParquetSchema pSchema)
        {
            List<Apache.Arrow.Field> fields = new List<Apache.Arrow.Field> ();

            foreach (var field in pSchema.DataFields)
            {
                var arrowType = DataTableHelpers.ConvertTypeToArrowType(field.ClrType);
                var arrowField = new Apache.Arrow.Field(field.Name, arrowType, field.IsNullable);
                fields.Add(arrowField);
            }

            return new Schema(fields,new Dictionary<string,string>());
        }

        internal static bool ValueEqual(Schema a, Schema b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return a == b;
            if (a.FieldsList.Count != b.FieldsList.Count) return false;

            for (int i = 0; i < a.FieldsList.Count; i++)
            {
                var fa = a.GetFieldByIndex(i);
                var fb = b.GetFieldByIndex(i);

                if (fa.Name != fb.Name) return false;
                if (fa.IsNullable != fb.IsNullable) return false;

                // Compare DataType value equality
                if (!DataTypesAreValueEqual(fa.DataType, fb.DataType))
                    return false;

                // Optional: metadata comparison (rarely used in your case)
                // if (!MetadataEqual(fa.Metadata, fb.Metadata)) return false;
            }

            return true;
        }

        private static bool DataTypesAreValueEqual(IArrowType a, IArrowType b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.GetType() != b.GetType()) return false;

            // Type-specific deep checks
            return a switch
            {
                //DecimalType da => da.Equals(b as DecimalType),          // checks Precision & Scale
                TimestampType ta => TimestampTypesAreValueEqual(ta, (b as TimestampType)!),
                //ListType la => DataTypesAreValueEqual(la.ValueType, (b as ListType).ValueType),
                StructType sa => StructTypesAreValueEqual(sa, (b as StructType)!),
                //MapType ma => MapTypesAreValueEqual(ma, b as MapType),
                //DictionaryType da => DictionaryTypesAreValueEqual(da, b as DictionaryType),
                _ => true  // most primitive types have no extra state
            };
        }

        private static bool TimestampTypesAreValueEqual(TimestampType a, TimestampType b)
        {
            //TODO: check values, unit, and timezone
            return true;
        }
        private static bool StructTypesAreValueEqual(StructType a, StructType b)
        {
            if (a.Fields.Count != b.Fields.Count) return false;
            for (int i = 0; i < a.Fields.Count; i++)
            {
                var fa = a.GetFieldByIndex(i);
                var fb = b.GetFieldByIndex(i);
                if (fa.Name != fb.Name || fa.IsNullable != fb.IsNullable)
                    return false;
                if (!DataTypesAreValueEqual(fa.DataType, fb.DataType))
                    return false;
            }
            return true;
        }
    }

    internal static class DataTableHelpers
    {
        internal static Type ConvertArrowTypeToType(IArrowType aType)
        {
            var parquetType = aType.TypeId switch
            {
                ArrowTypeId.Double => typeof(double),
                ArrowTypeId.Int64 => typeof(long),
                ArrowTypeId.Int32 => typeof(int),
                ArrowTypeId.Timestamp => typeof(DateTime),
                ArrowTypeId.Int8 => typeof(byte),
                _ => throw new NotSupportedException(aType.TypeId.ToString())
            };
            return parquetType;
        }
        
        internal static ArrowType ConvertTypeToArrowType(Type type)
        {
            if (type == typeof(double)) return new DoubleType();
            if (type == typeof(byte)) return new UInt8Type();
            if (type == typeof(DateTime)) return new TimestampType(TimeUnit.Millisecond, TimeZoneInfo.Utc);
            if (type == typeof(int)) return new Int32Type();
            if (type == typeof(long)) return new Int64Type();
            throw new NotSupportedException($"Unsupported Parquet type: {type}");
        }

        public static BarTable ConvertSeriesToBarTable(InstrumentDataSeries series)
        {
            if (series.Instrument == null)
                throw new ArgumentException("Series Instrument can not be null.");

            Schema barSchema = Bar.GetSchema();
            RecordBatch barBatch = Bar.EmptyBatch();

            var timeBuilder = new TimestampArray.Builder();
            var openBuilder = new DoubleArray.Builder();
            var highBuilder = new DoubleArray.Builder();
            var lowBuilder = new DoubleArray.Builder();
            var closeBuilder = new DoubleArray.Builder();
            var volumeBuilder = new Int64Array.Builder();

            foreach (var bar in series)
            {
                timeBuilder.Append(bar.Time);
                openBuilder.Append(bar.Open);
                highBuilder.Append(bar.High);
                lowBuilder.Append(bar.Low);
                closeBuilder.Append(bar.Close);
                volumeBuilder.Append(bar.Volume);
            }

            var batch = new RecordBatch(barSchema, new IArrowArray[]
            {
                timeBuilder.Build(),
                openBuilder.Build(),
                highBuilder.Build(),
                lowBuilder.Build(),
                closeBuilder.Build(),
                volumeBuilder.Build()
            }, series.Count);

            return new BarTable(series.Instrument, series.Interval, Table.TableFromRecordBatches(barSchema, new[] { batch }));
        }

        internal static (ParquetSchema Schema, DataColumn[] Data) ConvertArrowToParquet(Table arrowTable)
        {
            var parquetData = new List<DataColumn>(arrowTable.Schema.FieldsList.Count);

            ParquetSchema pSchema = SchemaHelpers.ConvertArrowToParquet(arrowTable.Schema);

            for (int i = 0; i < arrowTable.Schema.FieldsList.Count; i++)
            {
                var field = arrowTable.Schema.GetFieldByIndex(i);
                ChunkedArray chunked = arrowTable.Column(i).Data;

                var parquetField = pSchema.DataFields[i];

                DataColumn col = field.DataType.TypeId switch
                {
                    ArrowTypeId.Double => BuildDoubleColumn(chunked, parquetField),
                    ArrowTypeId.Int64 => BuildInt64Column(chunked, parquetField),
                    ArrowTypeId.Int32 => BuildInt32Column(chunked, parquetField),
                    ArrowTypeId.Timestamp => BuildTimestampColumn(chunked, parquetField),
                    ArrowTypeId.UInt8 => BuildUInt8Column(chunked, parquetField),
                    _ => throw new NotSupportedException(field.DataType.TypeId.ToString())
                };

                parquetData.Add(col);
            }

            return (pSchema, parquetData.ToArray());
        }

        private static DateTime? ConvertOffset(DateTimeOffset? offset) { if (offset != null && offset.HasValue) return offset.Value.UtcDateTime; return null; }

        internal static (ParquetSchema Schema, DataColumn[] Data) ConvertArrowBatchToParquet(RecordBatch arrowBatch)
        {
            var parquetFields = new List<DataField>(arrowBatch.Schema.FieldsList.Count);
            var parquetData = new List<System.Array>(arrowBatch.Schema.FieldsList.Count);

            for (int i = 0; i < arrowBatch.Schema.FieldsList.Count; i++)
            {
                var field = arrowBatch.Schema.GetFieldByIndex(i);
                IArrowArray colArray = arrowBatch.Column(i);

                var values = new List<object?>();

                for (int j = 0; j < colArray.Length; j++)
                {
                    object? val = colArray switch
                    {
                        DoubleArray da => da.GetValue(j),
                        Int64Array ia => ia.GetValue(j),
                        TimestampArray ta => ConvertOffset(ta.GetTimestamp(j)),
                        Int8Array ba => ba.GetValue(j),
                        _ => throw new NotSupportedException(colArray.GetType().Name)
                    };

                    values.Add(val);
                }

                var parquetType = ConvertArrowTypeToType(field.DataType);

                var parquetField = new DataField(field.Name, parquetType, field.IsNullable);
                var dataArray = values.ToArray();

                parquetFields.Add(parquetField);
                parquetData.Add(dataArray);
            }

            var parquetSchema = new ParquetSchema(parquetFields);
            var columns = new DataColumn[parquetFields.Count];
            for (int i = 0; i < columns.Length; i++)
            {
                columns[i] = new DataColumn(parquetFields[i], parquetData[i]);
            }

            return (parquetSchema, columns);
        }
        
        internal static Table ConvertParquetToArrow(DataColumn[] parquetColumns, ParquetSchema parquetSchema)
        {
            RecordBatch batch = ConvertParquetToArrowBatch(parquetColumns, parquetSchema);
            return Table.TableFromRecordBatches(batch.Schema, new[] { batch });
        }

        internal static RecordBatch ConvertParquetToArrowBatch(DataColumn[] parquetColumns, ParquetSchema parquetSchema)
        {
            if (parquetColumns == null || parquetColumns.Length == 0)
                throw new ArgumentException("No columns provided");

            int rowCount = parquetColumns[0].Data.Length;
            if (parquetColumns.Any(c => c.Data.Length != rowCount))
                throw new InvalidOperationException("All columns must have the same row count");

            var arrowArrays = new List<IArrowArray>(parquetColumns.Length);

            for (int i = 0; i < parquetColumns.Length; i++)
            {
                var col = parquetColumns[i];
                var field = col.Field;

                IArrowArray arrowArray;
                if (field.ClrType == typeof(DateTime))
                {
                    if (field.IsNullable) throw new Exception("Time field can not be nullable.");
                    arrowArray = BuildTimestampArray(col.Data);
                }
                else if (field.ClrType == typeof(long))
                    arrowArray = BuildInt64Array(col.Data, field.IsNullable);
                else if (field.ClrType == typeof(byte))
                    arrowArray = BuildUInt8Array(col.Data, field.IsNullable);
                else if (field.ClrType == typeof(double))
                    arrowArray = BuildDoubleArray(col.Data, field.IsNullable);
                else if (field.ClrType == typeof(int))
                    arrowArray = BuildInt32Array(col.Data, field.IsNullable);
                else
                    throw new NotSupportedException($"No builder for {field.ClrType}");

                arrowArrays.Add(arrowArray);
            }

            var arrowSchema = SchemaHelpers.ConvertParquetToArrow(parquetSchema);
            var recordBatch = new RecordBatch(arrowSchema, arrowArrays, rowCount);
            return  recordBatch;
        }

        private static TimestampArray BuildTimestampArray(System.Array valueArray)
        {
            var builder = new TimestampArray.Builder();
            builder.Reserve(valueArray.Length);

            var values = (DateTime[])valueArray;
            for (int i = 0; i < values.Length; i++)
            {
                builder.Append(new DateTimeOffset(values[i]));
            }

            return builder.Build();
        }

        private static DataColumn BuildTimestampColumn(ChunkedArray chunkedArray, DataField parquetField)
        {
            if (parquetField.IsNullable)
            {
                var values = new List<DateTime?>((int)chunkedArray.Length);
                for (int c = 0; c < chunkedArray.ArrayCount; c++)
                {
                    var arr = chunkedArray.ArrowArray(c) as TimestampArray
                        ?? throw new InvalidOperationException("Expected TimestampArray");

                    for (int j = 0; j < arr.Length; j++)
                    {
                        DateTime? dt = ConvertOffset(arr.GetTimestamp(j));
                        values.Add(dt);

                    }
                }
                return new DataColumn(parquetField, values.ToArray());
            }
            else
            {
                var values = new List<DateTime>((int)chunkedArray.Length);
                for (int c = 0; c < chunkedArray.ArrayCount; c++)
                {
                    var arr = chunkedArray.ArrowArray(c) as TimestampArray
                        ?? throw new InvalidOperationException("Expected TimestampArray");

                    for (int j = 0; j < arr.Length; j++)
                    {
                        DateTime? dt = ConvertOffset(arr.GetTimestamp(j));
                        if (dt == null || !dt.HasValue) throw new NullReferenceException();
                        values.Add(dt.Value);

                    }
                }
                return new DataColumn(parquetField, values.ToArray());
            }
        }

        private static DoubleArray BuildDoubleArray(System.Array valueArray, bool isNullable)
        {
            var builder = new DoubleArray.Builder();
            builder.Reserve(valueArray.Length);
            if (isNullable)
            {
                var values = (double?[])valueArray;
                for (int i = 0; i < values.Length; i++)
                    builder.Append(values[i]);
            }
            else
            {
                var values = (double[])valueArray;
                for (int i = 0; i < values.Length; i++)
                    builder.Append(values[i]);
            }

            return builder.Build();
        }

        private static DataColumn BuildDoubleColumn(ChunkedArray chunkedArray, DataField parquetField)
        {
            if (parquetField.IsNullable)
            {
                var values = new List<double?>((int)chunkedArray.Length);
                for (int c = 0; c < chunkedArray.ArrayCount; c++)
                {
                    var arr = chunkedArray.ArrowArray(c) as DoubleArray
                        ?? throw new InvalidOperationException("Expected DoubleArray");

                    for (int j = 0; j < arr.Length; j++)
                    {
                        double? dt = arr.GetValue(j);
                        values.Add(dt);
                    }
                }
                return new DataColumn(parquetField, values.ToArray());
            }
            else
            {
                var values = new List<double>((int)chunkedArray.Length);
                for (int c = 0; c < chunkedArray.ArrayCount; c++)
                {
                    var arr = chunkedArray.ArrowArray(c) as DoubleArray
                        ?? throw new InvalidOperationException("Expected DoubleArray");

                    for (int j = 0; j < arr.Length; j++)
                    {
                        double? dt = arr.GetValue(j);
                        if (dt == null || !dt.HasValue) throw new NullReferenceException();
                        values.Add(dt.Value);
                    }
                }
                return new DataColumn(parquetField, values.ToArray());
            }
        }

        private static Int64Array BuildInt64Array(System.Array valueArray, bool isNullable)
        {
            var builder = new Int64Array.Builder();
            builder.Reserve(valueArray.Length);
            if (isNullable)
            {
                var values = (long?[])valueArray;
                for (int i = 0; i < values.Length; i++)
                    builder.Append(values[i]);
            }
            else
            {
                var values = (long[])valueArray;
                for (int i = 0; i < values.Length; i++)
                    builder.Append(values[i]);
            }

            return builder.Build();
        }

        private static DataColumn BuildInt64Column(ChunkedArray chunkedArray, DataField parquetField)
        {
            if (parquetField.IsNullable)
            {
                var values = new List<long?>((int)chunkedArray.Length);
                for (int c = 0; c < chunkedArray.ArrayCount; c++)
                {
                    var arr = chunkedArray.ArrowArray(c) as Int64Array
                        ?? throw new InvalidOperationException("Expected Int64Array");

                    for (int j = 0; j < arr.Length; j++)
                    {
                        long? dt = arr.GetValue(j);
                        values.Add(dt);
                    }
                }
                return new DataColumn(parquetField, values.ToArray());
            }
            else
            {
                var values = new List<long>((int)chunkedArray.Length);
                for (int c = 0; c < chunkedArray.ArrayCount; c++)
                {
                    var arr = chunkedArray.ArrowArray(c) as Int64Array
                        ?? throw new InvalidOperationException("Expected Int64Array");

                    for (int j = 0; j < arr.Length; j++)
                    {
                        long? dt = arr.GetValue(j);
                        if (dt == null || !dt.HasValue) throw new NullReferenceException();
                        values.Add(dt.Value);
                    }
                }
                return new DataColumn(parquetField, values.ToArray());
            }
        }

        private static Int32Array BuildInt32Array(System.Array valueArray, bool isNullable)
        {
            var builder = new Int32Array.Builder();
            builder.Reserve(valueArray.Length);
            if (isNullable)
            {
                var values = (int?[])valueArray;
                for (int i = 0; i < values.Length; i++)
                    builder.Append(values[i]);
            }
            else
            {
                var values = (int[])valueArray;
                for (int i = 0; i < values.Length; i++)
                    builder.Append(values[i]);
            }

            return builder.Build();
        }

        private static DataColumn BuildInt32Column(ChunkedArray chunkedArray, DataField parquetField)
        {
            if (parquetField.IsNullable)
            {
                var values = new List<int?>((int)chunkedArray.Length);
                for (int c = 0; c < chunkedArray.ArrayCount; c++)
                {
                    var arr = chunkedArray.ArrowArray(c) as Int32Array
                        ?? throw new InvalidOperationException("Expected Int32Array");

                    for (int j = 0; j < arr.Length; j++)
                    {
                        int? dt = arr.GetValue(j);
                        values.Add(dt);
                    }
                }
                return new DataColumn(parquetField, values.ToArray());
            }
            else
            {
                var values = new List<int>((int)chunkedArray.Length);
                for (int c = 0; c < chunkedArray.ArrayCount; c++)
                {
                    var arr = chunkedArray.ArrowArray(c) as Int32Array
                        ?? throw new InvalidOperationException("Expected Int32Array");

                    for (int j = 0; j < arr.Length; j++)
                    {
                        int? dt = arr.GetValue(j);
                        if (dt == null || !dt.HasValue) throw new NullReferenceException();
                        values.Add(dt.Value);
                    }
                }
                return new DataColumn(parquetField, values.ToArray());
            }
        }

        private static UInt8Array BuildUInt8Array(System.Array valueArray, bool isNullable)
        {
            var builder = new UInt8Array.Builder();
            builder.Reserve(valueArray.Length);
            if (isNullable)
            {
                var values = (byte?[])valueArray;
                for (int i = 0; i < values.Length; i++)
                    builder.Append(values[i]);
            }
            else
            {
                var values = (byte[])valueArray;
                for (int i = 0; i < values.Length; i++)
                    builder.Append(values[i]);
            }

            return builder.Build();
        }

        private static DataColumn BuildUInt8Column(ChunkedArray chunkedArray, DataField parquetField)
        {
            if (parquetField.IsNullable)
            {
                var values = new List<byte?>((int)chunkedArray.Length);
                for (int c = 0; c < chunkedArray.ArrayCount; c++)
                {
                    var arr = chunkedArray.ArrowArray(c) as UInt8Array
                        ?? throw new InvalidOperationException("Expected UInt8Array");

                    for (int j = 0; j < arr.Length; j++)
                    {
                        byte? dt = arr.GetValue(j);
                        values.Add(dt);
                    }
                }
                return new DataColumn(parquetField, values.ToArray());
            }
            else
            {
                var values = new List<byte>((int)chunkedArray.Length);
                for (int c = 0; c < chunkedArray.ArrayCount; c++)
                {
                    var arr = chunkedArray.ArrowArray(c) as UInt8Array
                        ?? throw new InvalidOperationException("Expected UInt8Array");

                    for (int j = 0; j < arr.Length; j++)
                    {
                        byte? dt = arr.GetValue(j);
                        if (dt == null || !dt.HasValue) throw new NullReferenceException();
                        values.Add(dt.Value);
                    }
                }
                return new DataColumn(parquetField, values.ToArray());
            }
        }
    }
}
