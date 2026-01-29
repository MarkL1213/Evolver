using Parquet.Data;
using Parquet.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EvolverCore.Models
{
    public enum TableType : byte { Bar, Tick };

    public interface ICurrentTable
    {
        public int CurrentIndex { get; }
        public long RowCount { get; }

        public ColumnPointer<DateTime> Time { get; }
    }

    public enum TableLoadState { NotLoaded, Loading, Loaded, Error };

    internal class DataTableLoadStateChangeArgs : EventArgs
    {
        public DataTableLoadStateChangeArgs(TableLoadState state, DataTable? table)
        {
            State = state;
            Table = table;

        }
        
        public DataTable? Table { get; init; }
        public TableLoadState State { get; init; }
    }

    internal class BarTableLoadStateChangeArgs : EventArgs
    {
        public BarTableLoadStateChangeArgs(TableLoadState state, BarTable? table) { State = state; Table = table; }
        public BarTable? Table { get; init; }
        public TableLoadState State { get; init; }
    }

    public class BarTable : ICurrentTable
    {
        public BarTable(Instrument instrument, DataInterval interval, DataTable? table=null)
        {
            Instrument = instrument;
            Interval = interval;
            Accumulator = new DataAccumulator(interval);

            Table = table;
            Time = new ColumnPointer<DateTime>(this, Table?.Column("Time"));
            Open = new ColumnPointer<double>(this, Table?.Column("Open"));
            Close = new ColumnPointer<double>(this, Table?.Column("Close"));
            High = new ColumnPointer<double>(this, Table?.Column("High"));
            Low = new ColumnPointer<double>(this, Table?.Column("Low"));
            Bid = new ColumnPointer<double>(this, Table?.Column("Bid"));
            Ask = new ColumnPointer<double>(this, Table?.Column("Ask"));
            Volume = new ColumnPointer<long>(this, Table?.Column("Volume"));
        }

        internal event EventHandler<BarTableLoadStateChangeArgs>? BarTableLoadStateChange = null;

        internal void OnDataTableLoadStateChange(object? sender,DataTableLoadStateChangeArgs args)
        {
            TableLoadState oldState = State;
            if (args.State == TableLoadState.Loaded)
            {
                setTable(args.Table);
            }

            State = args.State;

            if (State != oldState) BarTableLoadStateChange?.Invoke(this, new BarTableLoadStateChangeArgs(State, this));
        }

        private void setTable(DataTable? table)
        {
            Table = table;

            Time = new ColumnPointer<DateTime>(this, Table?.Column("Time"));
            Open = new ColumnPointer<double>(this, Table?.Column("Open"));
            Close = new ColumnPointer<double>(this, Table?.Column("Close"));
            High = new ColumnPointer<double>(this, Table?.Column("High"));
            Low = new ColumnPointer<double>(this, Table?.Column("Low"));
            Bid = new ColumnPointer<double>(this, Table?.Column("Bid"));
            Ask = new ColumnPointer<double>(this, Table?.Column("Ask"));
            Volume = new ColumnPointer<long>(this, Table?.Column("Volume"));
        }

        public DataAccumulator Accumulator { get; init; }
        public Instrument Instrument { get; init; }
        public DataInterval Interval { get; init; }

        public TableLoadState State { get; private set; } = TableLoadState.NotLoaded;
        
        internal DataTable? Table { get; private set; } = null;

        public int CurrentIndex { get; private set; } = 0;

        public long RowCount { get { return Table != null ? Table.RowCount : 0; } }

        public DateTime MinTime { get { return Table != null ? Time[0] : DateTime.MinValue; }  }
        public DateTime MaxTime { get { return Table != null ? Time[(int)Table.RowCount - 1] : DateTime.MinValue; }  }

        public ColumnPointer<DateTime> Time { get; private set; }
        public ColumnPointer<double> Open { get; private set; }
        public ColumnPointer<double> Close { get; private set; }
        public ColumnPointer<double> High { get; private set; }
        public ColumnPointer<double> Low { get; private set; }
        public ColumnPointer<double> Bid { get; private set; }
        public ColumnPointer<double> Ask { get; private set; }
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

        public ColumnPointer<DateTime> Time { get; init; }
        public ColumnPointer<byte> Type { get; init; }
        public ColumnPointer<double> Value { get; init; }
    }

    public class DataTablePointer
    {
        public DataTablePointer(ICurrentTable table, DateTime start, DateTime end)
        {
            if (start > end)
                throw new ArgumentException($"Start  must be less than or equal to end: start={start} end={end}");

            _table = table;
            CurrentBar = 0;

            CalculateOffsets(start,end);
        }

        public DataTablePointer(ICurrentTable table)
        {

            _table = table;
            CurrentBar = 0;

            _startOffset = 0;
            _startTime = table.Time[0];

            _endoffset = (int)_table.RowCount - 1;
            _endTime = table.Time[_endoffset];
        }

        internal void CalculateOffsets(DateTime start, DateTime end)
        {
            _startTime = start;
            _endTime = end;

            //TODO: find indexes of start and end
        }


        int _startOffset;
        DateTime _startTime;

        int _endoffset;
        DateTime _endTime;

        ICurrentTable _table;

        public int CurrentBar { get; private set; }

        
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

        public DataTable DynamicSlice()
        {
            lock (_lock)
            {
                List<IDataTableColumn> newColumns = new List<IDataTableColumn>();
                
                DataTable sliceTable = new DataTable(Schema, 0);

                for (int i = 0; i < _columns.Count; i++)
                {
                    IDataTableColumn srcColumn = _columns[i];
                    IDataTableColumn newCol = srcColumn.ExportDynamics();
                    newColumns.Add(newCol);
                }

                sliceTable._columns = newColumns;
                return sliceTable;
            }
        }

        public DataTable Slice(int index, int length)
        {
            lock (_lock)
            {
                List<IDataTableColumn> newColumns = new List<IDataTableColumn>();
                DataTable sliceTable = new DataTable(Schema, 0);

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
}
