using Avalonia.Controls.ApplicationLifetimes;
using NP.Utilities;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public static ParquetSchema GetSchema()
        {
            List<DataField> fields = new List<DataField>();

            fields.Add(new DataField("Time", typeof(DateTime)));
            fields.Add(new DataField("Type", typeof(byte)));
            fields.Add(new DataField("Value", typeof(double)));

            return new ParquetSchema(fields);
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

        public static ParquetSchema GetSchema()
        {
            List<DataField> fields = new List<DataField>();

            fields.Add(new DataField("Time", typeof(DateTime)));
            fields.Add(new DataField("Open", typeof(double)));
            fields.Add(new DataField("High", typeof(double)));
            fields.Add(new DataField("Low", typeof(double)));
            fields.Add(new DataField("Close", typeof(double)));
            fields.Add(new DataField("Volume", typeof(long)));

            return new ParquetSchema(fields);
        }
    }

    public class ColumnPointer<T> where T : struct
    {
        private IDataTableColumn _column;
        private ICurrentTable _parentTable;


        internal ColumnPointer(ICurrentTable parentTable, IDataTableColumn column)
        {
            //match T vs column.DataType

            _column = column;
            _parentTable = parentTable;
        }

        public T GetValueAt(int index)
        {
            if (index < 0 || index >= _column.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return (T)_column.GetValueAt(index);
        }

        public T this[int barsAgo]
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
        public long RowCount { get; }
    }

    public class BarTable : ICurrentTable
    {
        public BarTable(Instrument instrument, DataInterval interval, DataTable table)
        {
            Instrument = instrument;
            Interval = interval;
            Table = table;
            CurrentIndex = 0;

            Time = new ColumnPointer<DateTime>(this, Table.Column("Time"));
            Open = new ColumnPointer<double>(this, Table.Column("Open"));
            Close = new ColumnPointer<double>(this, Table.Column("Close"));
            High = new ColumnPointer<double>(this, Table.Column("High"));
            Low = new ColumnPointer<double>(this, Table.Column("Low"));
            Volume = new ColumnPointer<long>(this, Table.Column("Volume"));

        }

        public Instrument Instrument { get; init; }
        public DataInterval Interval { get; init; }
        internal DataTable Table { get; init; }

        public int CurrentIndex { get; private set; }

        public long RowCount { get { return Table.RowCount; } }

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
        public TickTable(Instrument instrument, DataTable table)
        {
            Instrument = instrument;
            Table = table;

            Time = new ColumnPointer<DateTime>(this, Table.Column("Time"));
            Type = new ColumnPointer<byte>(this, Table.Column("Type"));
            Value = new ColumnPointer<double>(this, Table.Column("Value"));
        }

        public Instrument Instrument { get; init; }
        public DataTable Table { get; init; }

        public int CurrentIndex { get; private set; }

        public long RowCount { get { return Table.RowCount; } }

        public ColumnPointer<DateTime> Time { get; private set; }
        public ColumnPointer<byte> Type { get; private set; }
        public ColumnPointer<double> Value { get; private set; }
    }

    public enum DataType { Int32, Int64, UInt8, Double, DateTime };

    public interface IDataTableColumn
    {
        public DataType DataType { get; }
        public int Count { get; }
        
        public int[] Offsets { get; }

        public List<Array> Data { get; }
        public string Name { get; }
        public Array Series { get; }

        public object GetValueAt(int index);

        public Array ToArray();

        public IDataTableColumn ExportRange(int index, int length);
        public void AddDataColumn(DataColumn column);
        public void AddDataColumn(IDataTableColumn column);

        public void SetValues(List<object> values);

    }

    public static class DataTableColumnFactory
    {
        public static DataTableColumn<T> CopyBlankTableColumn<T>(IDataTableColumn sourceColumn)
        {
            DataTableColumn<T> c = new DataTableColumn<T>(sourceColumn.Name, sourceColumn.DataType, sourceColumn.Count);
            return c;
        }
    }



    public class DataTableColumn<T> : IDataTableColumn
    {
        public DataTableColumn(string name, DataType dataType,int columnSize)
        {
            Name = name;
            DataType = dataType;
            _series = new List<T>(columnSize);
        }

        List<Array> _dataArrays = new List<Array>();
        int[] _cumulativeOffsets = new int[0];
        List<T> _series;

        public string Name { get; private set; } = string.Empty;
        public DataType DataType { get; private set; } = DataType.Int32;
        public int Count { get { return RowCount(); } }

        public int[] Offsets { get { return _cumulativeOffsets; } }

        public List<Array> Data { get { return _dataArrays; } }

        public Array Series { get { return _series.ToArray(); } }

        public void AddDataColumn(DataColumn column)
        {
            if (_series.Count > 0) throw new Exception("Target series can not have an un-serialized data chunk.");

            int n = _cumulativeOffsets.Length > 0 ? _cumulativeOffsets[_cumulativeOffsets.Length - 1] : 0;
            
            List<int> offsetList = _cumulativeOffsets.ToList<int>();
            offsetList.Add(n + column.Data.Length);
            _cumulativeOffsets = offsetList.ToArray();
            
            _dataArrays.Add(column.Data);
        }

        public void AddDataColumn(IDataTableColumn column)
        {
            if (_series.Count > 0) throw new Exception("Target series can not have an un-serialized data chunk.");

            int n = _cumulativeOffsets.Length > 0 ? _cumulativeOffsets[_cumulativeOffsets.Length - 1] : 0;

            List<int> offsetList = _cumulativeOffsets.ToList<int>();
            offsetList.AddRange(column.Offsets.Select(i => i + n));
            _cumulativeOffsets = offsetList.ToArray();
          
            _dataArrays.AddRange(column.Data);
            _series.AddRange((T[])column.Series);
        }

        public void InitValues(List<object> values)
        {
            Array a = values.ToArray();
            _dataArrays.Add(a);
        }

        public IDataTableColumn ExportRange(int index, int length)
        {
            IDataTableColumn newCol = DataTableColumnFactory.CopyBlankTableColumn<T>(this);

            List<object> values = new List<object>();
            for (int i = index; i < index + length; i++)
                values.Add(GetValueAt(i));

            newCol.SetValues(values);
            return newCol;
        }

        public void SetValues(List<object> values)
        {
            _series.Clear();
            _dataArrays.Clear();

            InitValues(values);
        }

        public int RowCount()
        {
            int n = 0;
            foreach (Array a in _dataArrays) n += a.Length;
            n += _series.Count;
            return n;
        }


        public object GetValueAt(int index)
        {
            //determine where index points based on offsets
            //return offset shifted index from correct array/list
            int i = Array.BinarySearch(_cumulativeOffsets, index);
            if (i < 0) i = ~i;

            if (i == _cumulativeOffsets.Length - 1)
                return i >= 1 ? _series[index - _cumulativeOffsets[i - 1]]! : _series[index]!;
            else
            {
                Array a = _dataArrays[i];
                return i >= 1 ? a.GetValue(index - _cumulativeOffsets[i - 1])! : a.GetValue(index)!;
            }
        }

        public void SetValueAt(T value, int index)
        {
            int i = Array.BinarySearch(_cumulativeOffsets, index);
            if (i < 0) i = ~i;

            if (i == _cumulativeOffsets.Length - 1)
            {
                if (i >= 1) _series[index - _cumulativeOffsets[i - 1]] = value;
                else _series[index] = value;
            }
            else
            {
                Array a = _dataArrays[i];
                if (i >= 1) a.SetValue(value, index - _cumulativeOffsets[i - 1]);
                else a.SetValue(value, index);
            }
        }

        public System.Array ToArray()
        {
            return _series.ToArray();
        }

        public T this[int index]
        {
            get { return (T)GetValueAt(index); }
            internal set { SetValueAt(value, index); }
        }
    }

    public class DataTable
    {
        List<IDataTableColumn> _columns;
        ParquetSchema _pSchema;
        object _lock = new object();

        public DataTable(ParquetSchema pSchema, int columnSize)
        {
            lock (_lock)
            {
                _pSchema = pSchema;
                _columns = new List<IDataTableColumn>();
                createColumnsFromParquetSchema(columnSize);
            }
        }

        public int RowCount { get { lock (_lock) { return _columns.Count > 0 ? _columns[0].Count : 0; } } }

        public ParquetSchema Schema { get { lock (_lock) { return _pSchema; } } }

        public IDataTableColumn? Column(string name)
        {
            lock (_lock) { return _columns.FirstOrDefault(c => c.Name == name); }
        }

        private List<IDataTableColumn> Columns { get { return _columns; } }

        public DataColumn ExportDataColumn(string name)
        {
            lock (_lock)
            {
                IDataTableColumn? col = Column(name);
                if (col == null) throw new ArgumentException();
                DataField? colField = null;

                foreach (DataField field in _pSchema.DataFields)
                {
                    if (field.Name == name) { colField = field; break; }
                }
                if (colField == null) throw new ArgumentException();

                return new DataColumn(colField, col.ToArray());
            }
        }

        private void createColumnsFromParquetSchema(int columnSize)
        {
            lock (_lock)
            {
                _columns.Clear();

                foreach (DataField field in _pSchema.DataFields)
                {
                    if (field.ClrType == typeof(DateTime))
                        _columns.Add(new DataTableColumn<DateTime>(field.Name, DataType.DateTime, columnSize));
                    else if (field.ClrType == typeof(double))
                        _columns.Add(new DataTableColumn<double>(field.Name, DataType.Double, columnSize));
                    else if (field.ClrType == typeof(long))
                        _columns.Add(new DataTableColumn<long>(field.Name, DataType.Int64, columnSize));
                    else if (field.ClrType == typeof(int))
                        _columns.Add(new DataTableColumn<int>(field.Name, DataType.Int32, columnSize));
                    else if (field.ClrType == typeof(byte))
                        _columns.Add(new DataTableColumn<byte>(field.Name, DataType.UInt8, columnSize));
                    else
                    {
                        _columns.Clear();
                        throw new Exception($"Unhandled parquet column type {field.ClrType.ToString()}");
                    }
                }
            }
        }

        public bool CompareColumnStructure(DataTable table)
        {
            lock (_lock)
            {
                if (table._columns.Count != _columns.Count) return false;
                for (int i = 0; i < table._columns.Count; i++)
                {
                    IDataTableColumn c = table._columns[i];
                    if (c.Name != _columns[i].Name) return false;
                    if (c.DataType != _columns[i].DataType) return false;
                }

                return true;
            }
        }

        public bool CompareColumnStructure(DataColumn[] columns)
        {
            lock (_lock)
            {
                if (columns.Length != _columns.Count) return false;
                for (int i = 0; i < columns.Length; i++)
                {
                    DataColumn c = columns[i];
                    if (c.Field.Name != _columns[i].Name) return false;
                    switch (_columns[i].DataType)
                    {
                        case DataType.DateTime: if (c.Field.ClrType != typeof(DateTime)) return false; break;
                        case DataType.Int64: if (c.Field.ClrType != typeof(long)) return false; break;
                        case DataType.Int32: if (c.Field.ClrType != typeof(int)) return false; break;
                        case DataType.UInt8: if (c.Field.ClrType != typeof(byte)) return false; break;
                        case DataType.Double: if (c.Field.ClrType != typeof(double)) return false; break;
                        default: return false;
                    }
                }
                return true;
            }
        }

        public void AddColumnData(DataColumn[] columns)
        {
            lock (_lock)
            {
                if (!CompareColumnStructure(columns)) throw new ArgumentException();

                //TODO: be sure table is aligned to current values
                //column.last < _series.first and column.first > lastArrary.last
                //lastArray.last + 1 interval == column.first


                for (int i = 0; i < columns.Length; i++)
                {
                    _columns[i].AddDataColumn(columns[i]);
                }
            }
        }

        public void AppendTable(DataTable table)
        {
            lock (_lock)
            {
                if (!CompareColumnStructure(table)) throw new ArgumentException();

                //TODO:verify alignment


                for (int i = 0; i < _columns.Count; i++)
                {
                    _columns[i].AddDataColumn(table._columns[i]);
                }
            }
        }

        public DataTable Slice(int index, int length)
        {
            lock (_lock)
            {
                List<IDataTableColumn> newColumns = new List<IDataTableColumn>();
                DataTable sliceTable = new DataTable(Schema, length);

                for (int i = 0; i < _columns.Count; i++)
                {
                    IDataTableColumn srcColumn = _columns[i];
                    IDataTableColumn newCol = srcColumn.ExportRange(index, length);
                    newColumns.Add(newCol);
                }

                sliceTable._columns = newColumns;
                return sliceTable;
            }
        }
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

        public static async Task<ICurrentTable> ReadToDataTableAsync(Instrument instrument, DataInterval interval, DateTime start, DateTime end)
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

            DataTable? table = null;

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

                DataTable subTable = await ReadToDataTableAsync(filePath, tableType, rowGroups);

                if (table == null) table = subTable;
                else table.AppendTable(subTable);
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

        private static async Task<DataTable> ReadToDataTableAsync(string filePath, TableType tableType, int[]? rowGroups = null)
        {
            ParquetSchema schema;
            switch (tableType)
            {
                case TableType.Bar: schema = Bar.GetSchema(); break;
                case TableType.Tick: schema = Tick.GetSchema(); break;
                default: throw new ArgumentException("Unrecognized table type.", nameof(tableType));
            }

            if (!File.Exists(filePath)) return new DataTable(schema, 0);
            
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

                DataTable resultTable = new DataTable(schema, 0);

                foreach (int rowGroupIndex in rowGroupsToRead)
                {
                    DataColumn[] columns = await reader.ReadEntireRowGroupAsync(rowGroupIndex);
                    
                    resultTable.AddColumnData(columns);
                }

                return resultTable;
            }
        }

        internal static async Task WritePartitionedBars(DataTable table, Instrument instrument, DataInterval interval)
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

                DataTable subTable = table.Slice(group.Index,group.Length);

                await WriteDataTableAsync(
                    GetBarPartitionPath(instrument, interval, date),
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
            if (!File.Exists(filePath)) File.Create(filePath);

            using (FileStream stream = new FileStream(filePath, FileMode.Truncate))
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
    }

    internal static class SchemaHelpers
    {
        internal static bool ValueEqual(ParquetSchema a, ParquetSchema b)
        {
            return a.Equals(b);

            //if (ReferenceEquals(a, b)) return true;
            //if (a == null || b == null) return a == b;
            //if (a.FieldsList.Count != b.FieldsList.Count) return false;

            //for (int i = 0; i < a.FieldsList.Count; i++)
            //{
            //    var fa = a.GetFieldByIndex(i);
            //    var fb = b.GetFieldByIndex(i);

            //    if (fa.Name != fb.Name) return false;
            //    if (fa.IsNullable != fb.IsNullable) return false;

            //    // Compare DataType value equality
            //    if (!DataTypesAreValueEqual(fa.DataType, fb.DataType))
            //        return false;

            //    // Optional: metadata comparison (rarely used in your case)
            //    // if (!MetadataEqual(fa.Metadata, fb.Metadata)) return false;
            //}

            //return true;
        }
    }

    internal static class DataTableHelpers
    {
        internal static Type ConvertDataTypeToType(DataType aType)
        {
            var clrType = aType switch
            {
                DataType.Double => typeof(double),
                DataType.Int64 => typeof(long),
                DataType.Int32 => typeof(int),
                DataType.DateTime => typeof(DateTime),
                DataType.UInt8 => typeof(byte),
                _ => throw new NotSupportedException(aType.ToString())
            };
            return clrType;
        }

        //Ideally this is temporary as the transition progresses. Ultimately only DataTable should be the norm and Series objects will get refactored.
        public static BarTable ConvertSeriesToBarTable(InstrumentDataSeries series)
        {
            if (series.Instrument == null)
                throw new ArgumentException("Series Instrument can not be null.");

            ParquetSchema barSchema = Bar.GetSchema();
            DataTable table = new DataTable(barSchema, series.Count);

            DataTableColumn<DateTime> timeCol = table.Column("Time") as DataTableColumn<DateTime> ?? throw new NullReferenceException();
            DataTableColumn<double> openCol = table.Column("Open") as DataTableColumn<double> ?? throw new NullReferenceException();
            DataTableColumn<double> highCol = table.Column("High") as DataTableColumn<double> ?? throw new NullReferenceException();
            DataTableColumn<double> lowCol = table.Column("Low") as DataTableColumn<double> ?? throw new NullReferenceException();
            DataTableColumn<double> closeCol = table.Column("Close") as DataTableColumn<double> ?? throw new NullReferenceException();
            DataTableColumn<long> volumeCol = table.Column("Volume") as DataTableColumn<long> ?? throw new NullReferenceException();



            for (int i = 0; i < series.Count; i++)
            {
                timeCol[i] = series[i].Time;
                openCol[i] = series[i].Open;
                highCol[i] = series[i].High;
                lowCol[i] = series[i].Low;
                closeCol[i] = series[i].Close;
                volumeCol[i] = series[i].Volume;
            }



            return new BarTable(series.Instrument, series.Interval, table);
        }

        internal static (ParquetSchema Schema, DataColumn[] Data) ConvertDataTableToParquet(DataTable table)
        {
            int fieldCount = table.Schema.DataFields.Length;
            var parquetData = new List<DataColumn>(fieldCount);

            for (int i = 0; i < fieldCount; i++)
            {
                var field = table.Schema.DataFields[i];
                IDataTableColumn? column = table.Column(field.Name);
                if (column == null) throw new ArgumentException();
                DataColumn col = new DataColumn(field, column.ToArray());
                parquetData.Add(col);
            }

            return (table.Schema, parquetData.ToArray());
        }
    }
}
