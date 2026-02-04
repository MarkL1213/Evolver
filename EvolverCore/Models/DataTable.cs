using Parquet.Data;
using Parquet.File.Values.Primitives;
using Parquet.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;

namespace EvolverCore.Models
{
    public enum TableType : byte { Bar, Tick };

    //public interface ICurrentTable
    //{
    //    public int CurrentIndex { get; }
    //    public long RowCount { get; }

    //    public bool IsLive { get; }

    //    public void AddTick(DateTime time, double bid, double ask, long volume);

    //    public ColumnPointer<DateTime> Time { get; }
    //}

    public enum TableLoadState { NotLoaded, Loading, Loaded, Error };

    //internal class DataTableLoadStateChangeArgs : EventArgs
    //{
    //    public DataTableLoadStateChangeArgs(TableLoadState state, DataTable? table)
    //    {
    //        State = state;
    //        Table = table;
    //    }
        
    //    public DataTable? Table { get; init; }
    //    public TableLoadState State { get; init; }
    //}

    internal class BarTablePointerLoadStateChangeArgs : EventArgs
    {
        public BarTablePointerLoadStateChangeArgs(TableLoadState state, BarTablePointer? table) { State = state; Table = table; }
        public BarTablePointer? Table { get; init; }
        public TableLoadState State { get; init; }
    }

    public class BarTable
    {
        private List<BarTablePointer> _registeredPointers = new List<BarTablePointer>();
        internal List<BarTablePointer> RegisteredPointers { get {  return _registeredPointers; } }

        public BarTable(DataTable table)
        {
            Table = table;

            DataTableColumn<DateTime>? timeCol = Table!.Column("Time") as DataTableColumn<DateTime>;
            if (timeCol == null)
                throw new ArgumentException("BarTable source table does not contain a 'Time' column.");
            Time = timeCol;

            DataTableColumn<double>? openCol = Table!.Column("Open") as DataTableColumn<double>;
            if (openCol == null)
                throw new ArgumentException("BarTable source table does not contain a 'Open' column.");
            Open = openCol;

            DataTableColumn<double>? closeCol = Table!.Column("Close") as DataTableColumn<double>;
            if (closeCol == null)
                throw new ArgumentException("BarTable source table does not contain a 'Close' column.");
            Close = closeCol;

            DataTableColumn<double>? highCol = Table!.Column("High") as DataTableColumn<double>;
            if (highCol == null)
                throw new ArgumentException("BarTable source table does not contain a 'High' column.");
            High = highCol;

            DataTableColumn<double>? lowCol = Table!.Column("Low") as DataTableColumn<double>;
            if (lowCol == null)
                throw new ArgumentException("BarTable source table does not contain a 'Low' column.");
            Low = lowCol;

            DataTableColumn<double>? bidCol = Table!.Column("Bid") as DataTableColumn<double>;
            if (bidCol == null)
                throw new ArgumentException("BarTable source table does not contain a 'Bid' column.");
            Bid = bidCol;

            DataTableColumn<double>? askCol = Table!.Column("Ask") as DataTableColumn<double>;
            if (askCol == null)
                throw new ArgumentException("BarTable source table does not contain a 'Ask' column.");
            Ask = askCol;

            DataTableColumn<long>? volumeCol = Table!.Column("Volume") as DataTableColumn<long>;
            if (volumeCol == null)
                throw new ArgumentException("BarTable source table does not contain a 'Volume' column.");
            Volume = volumeCol;
        }

        public Instrument? Instrument { get { return Table?.Instrument; } }
        public DataInterval? Interval { get { return Table?.Interval; } }

        internal DataTable Table { get; init; }

        public long RowCount { get { return Table != null ? Table.RowCount : 0; } }

        public DateTime MinTime { get { return Table != null ? Time.GetValueAt(0) : DateTime.MinValue; } }
        public DateTime MaxTime { get { return Table != null ? Time.GetValueAt((int)Table.RowCount - 1) : DateTime.MinValue; } }

        public DataTableColumn<DateTime> Time { get; private set; }
        public DataTableColumn<double> Open { get; private set; }
        public DataTableColumn<double> Close { get; private set; }
        public DataTableColumn<double> High { get; private set; }
        public DataTableColumn<double> Low { get; private set; }
        public DataTableColumn<double> Bid { get; private set; }
        public DataTableColumn<double> Ask { get; private set; }
        public DataTableColumn<long> Volume { get; private set; }

        public bool IsLive { get; private set; } = false;


        public void AddTick(DateTime time, double bid, double ask, long volume)
        {//Runs in the context of the connection data update worker

        }
        public void StartNewRow(DateTime time)
        {//Runs in the context of the connection data update worker

        }

        internal void RegisterPointer(BarTablePointer pointer)
        {
            if(_registeredPointers.Contains(pointer)) return;
            _registeredPointers.Add(pointer);
        }

        internal void UnRegisterPointer(BarTablePointer pointer)
        {
            if (_registeredPointers.Contains(pointer)) _registeredPointers.Remove(pointer);
        }

        internal static BarTable GenerateRandomData(Instrument randomInstrument, DataInterval interval, DateTime startTime, int size, int seed)
        {
            DataTable dt = new DataTable(Bar.GetSchema(), size, TableType.Bar, randomInstrument, interval);
            BarTable bt = new BarTable(dt);
            //add random data to dt here...
            Random r = new Random(seed);
            DateTime start = startTime;

            int lastClose = -1;
            for (int i = 0; i < size; i++)
            {
                int open = lastClose == -1 ? r.Next(10, 100) : lastClose;
                int close = r.Next(10, 100);
                int volume = r.Next(100, 1000);
                int high = open > close ? open + r.Next(0, 15) : close + r.Next(0, 15);
                int low = open > close ? close - r.Next(0, 15) : open - r.Next(0, 15);

                bt.Time.SetValueAt(start, i);
                bt.Open.SetValueAt(open, i);
                bt.Close.SetValueAt(close, i);
                bt.High.SetValueAt(high, i);
                bt.Low.SetValueAt(low, i);
                bt.Volume.SetValueAt(volume, i);

                bt.Bid.SetValueAt(open, i);
                bt.Ask.SetValueAt(open, i);

                lastClose = close;
                start = interval.Add(start, 1);
            }

            return bt;
        }
    }

    //public class TickTable
    //{
    //    public TickTable(Instrument instrument, DataTable? table)
    //    {
    //        Instrument = instrument;
    //        Table = table;

    //        Time = new ColumnPointer<DateTime>(this, Table?.Column("Time"));
    //        Type = new ColumnPointer<byte>(this, Table?.Column("Type"));
    //        Value = new ColumnPointer<double>(this, Table?.Column("Value"));
    //    }

    //    public Instrument Instrument { get; init; }
    //    public DataTable? Table { get; init; }

    //    public int CurrentIndex { get; private set; } = -1;

    //    public long RowCount { get { return Table != null ? Table.RowCount : 0; } }

    //    public ColumnPointer<DateTime> Time { get; init; }
    //    public ColumnPointer<byte> Type { get; init; }
    //    public ColumnPointer<double> Value { get; init; }

    //    public bool IsLive { get; private set; } = false;

    //    public void AddTick(DateTime time, double bid, double ask, long volume)
    //    {//Runs in the context of the connection data update worker

    //    }
    //    public void StartNewRow(DateTime time)
    //    {//Runs in the context of the connection data update worker

    //    }
    //}

    public class BarTablePointer : IDisposable
    {
        public BarTablePointer(BarTable table, Instrument instrument, DataInterval interval, DateTime start, DateTime end)
        {
            if (start > end)
                throw new ArgumentException($"Start  must be less than or equal to end: start={start} end={end}");

            _table = table;

            _table.RegisterPointer(this);

            CurrentBar = -1;
            State = TableLoadState.Loaded;
            Instrument = instrument;
            Interval = interval;


            Time = new ColumnPointer<DateTime>(this, table.Time);
            Open = new ColumnPointer<double>(this, table.Open);
            Close = new ColumnPointer<double>(this, table.Close);
            High = new ColumnPointer<double>(this, table.High);
            Low = new ColumnPointer<double>(this, table.Low);
            Bid = new ColumnPointer<double>(this, table.Bid);
            Ask = new ColumnPointer<double>(this, table.Ask);
            Volume = new ColumnPointer<long>(this, table.Volume);

            CalculateOffsets(start, end);
        }

        public BarTablePointer(BarTable? table, Instrument instrument, DataInterval interval)
        {
            _table = table;
            CurrentBar = -1;
            Instrument = instrument;
            Interval = interval;

            if (_table != null)
            {
                State = TableLoadState.Loaded;
                _table.RegisterPointer(this);
            }

            Time = new ColumnPointer<DateTime>(this, table?.Time);
            Open = new ColumnPointer<double>(this, table?.Open);
            Close = new ColumnPointer<double>(this, table?.Close);
            High = new ColumnPointer<double>(this, table?.High);
            Low = new ColumnPointer<double>(this, table?.Low);
            Bid = new ColumnPointer<double>(this, table?.Bid);
            Ask = new ColumnPointer<double>(this, table?.Ask);
            Volume = new ColumnPointer<long>(this, table?.Volume);

            StartOffset = 0;
            EndOffset = _table == null ? 0 : (int)_table.RowCount - 1;
        }

        public Instrument Instrument { get; private set; }
        public DataInterval Interval { get; private set; }

        public TableLoadState State { get; private set; } = TableLoadState.NotLoaded;

        internal event EventHandler<BarTablePointerLoadStateChangeArgs>? LoadStateChange = null;

        private string _errorMessage = string.Empty;

        internal double LowestLow()
        {
            if (RowCount == 0) return 0;
            double v = double.MaxValue;
            for (int i = 0; i < RowCount; i++)
            { if (Low.GetValueAt(i) < v) v = Low.GetValueAt(i); }
            return v;
        }

        internal double HighestHigh()
        {
            if (RowCount == 0) return 0;
            double v = double.MinValue;
            for (int i = 0; i < RowCount; i++)
            { if (High.GetValueAt(i) > v) v = High.GetValueAt(i); }
            return v;
        }

        internal void OnDataTableLoaded(object? sender, DataLoadJobDoneArgs args)
        {
            TableLoadState oldState = State;

            if (!args.HasError)
            {
                SetTable(args.ResultTable, args.SourceJob.StartTime, args.SourceJob.EndTime);
                State = TableLoadState.Loaded;
            }
            else
            {
                _errorMessage = args.ErrorMessage;
                State = TableLoadState.Error;
            }

            if (State != oldState) LoadStateChange?.Invoke(this, new BarTablePointerLoadStateChangeArgs(State, this));
        }

        internal void SetTable(BarTable? table, DateTime start, DateTime end)
        {
            if (_table != null)
                _table.UnRegisterPointer(this);

            _table = table;

            if (_table != null)
                _table.RegisterPointer(this);

            CurrentBar = -1;

            Time = new ColumnPointer<DateTime>(this, table?.Time);
            Open = new ColumnPointer<double>(this, table?.Open);
            Close = new ColumnPointer<double>(this, table?.Close);
            High = new ColumnPointer<double>(this, table?.High);
            Low = new ColumnPointer<double>(this, table?.Low);
            Bid = new ColumnPointer<double>(this, table?.Bid);
            Ask = new ColumnPointer<double>(this, table?.Ask);
            Volume = new ColumnPointer<long>(this, table?.Volume);

            if (_table == null)
            {
                StartOffset = 0;
                EndOffset = 0;
            }
            else
            {
                Instrument = _table.Instrument!;
                Interval = (DataInterval)_table.Interval!;

                CalculateOffsets(start, end);
            }
        }

        internal void ResetCurrentBar() { CurrentBar = -1; }
        internal bool CurrentIsEnd() { return CurrentBar == EndOffset; }
        internal void IncrementCurrentBar() { CurrentBar++; }

        public DateTime MinTime(int lastCount)
        {
            if (RowCount == 0) return DateTime.MinValue;

            DateTime result = DateTime.MaxValue;
            int n = 1;
            for (int i = EndOffset; i >= StartOffset; i--)
            {
                if (n++ > lastCount) return result;
                if (Time.GetValueAt(i) < result) result = Time.GetValueAt(i);
            }

            return result;
        }
        public DateTime MaxTime(int lastCount)
        {
            if (RowCount == 0) return DateTime.MaxValue;

            DateTime result = DateTime.MinValue;
            int n = 1;
            for (int i = EndOffset; i >= StartOffset; i--)
            {
                if (n++ > lastCount) return result;
                if (Time.GetValueAt(i) > result) result = Time.GetValueAt(i);
            }

            return result;
        }

        public double CalculatePriceField(int barsAgo, BarPriceValue priceField)
        {
            //FIXME : CalculatePriceField(int barsAgo, BarPriceValue priceField)
            return 0;
        }

        public BarTablePointer Slice(DateTime min, DateTime max)
        {
            if (_table == null) throw new NullReferenceException("Unable to slice non-existing table.");

            if (min < Time[0] || max > Time[(int)_table.RowCount - 1])
                throw new ArgumentOutOfRangeException("Slice dates out of range.");

            return new BarTablePointer(_table, _table.Instrument!, (DataInterval)_table.Interval!, min, max);
        }

        internal void CalculateOffsets(DateTime start, DateTime end)
        {
            int startIndex = Time.RawFindIndex(start);
            int endIndex = Time.RawFindIndex(end);

            if (startIndex == -1 || endIndex == -1)
                throw new EvolverException("Unable to locate start/end time index values.");

            StartOffset = startIndex;
            EndOffset = endIndex;
        }

        internal int StartOffset { get; private set; }
        internal int EndOffset { get; private set; }

        BarTable? _table;
        private bool disposedValue;

        public int CurrentBar { get; private set; }

        public int RowCount { get { return EndOffset - StartOffset; } }

        public ColumnPointer<DateTime> Time { get; private set; }
        public ColumnPointer<double> Open { get; private set; }
        public ColumnPointer<double> Close { get; private set; }
        public ColumnPointer<double> High { get; private set; }
        public ColumnPointer<double> Low { get; private set; }
        public ColumnPointer<double> Bid { get; private set; }
        public ColumnPointer<double> Ask { get; private set; }
        public ColumnPointer<long> Volume { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_table != null) _table.UnRegisterPointer(this);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~BarTablePointer()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class DataTableBase
    {
        internal List<IDataTableColumn> Columns { get; set; } = new List<IDataTableColumn>();
        private object _lock = new object();
        internal DataTableBase() { }

        public IDataTableColumn? Column(string name)
        {
            lock (_lock) { return Columns.FirstOrDefault(c => c.Name == name); }
        }

        public int RowCount { get { lock (_lock) { return Columns.Count > 0 ? Columns[0].Count : 0; } } }

        public void AddColumn(string name)
        {
            int size = 0;
            if (Columns.Count > 0) size = Columns[0].Count;

            Columns.Add(new DataTableColumn<double>(name, DataType.Double, size));
        }

        public DataTableColumn<double> this[int colIndex]
        {
            get
            {
                DataTableColumn<double>? c = Columns[colIndex] as DataTableColumn<double>;
                if (c == null)
                    throw new EvolverException("DataTableBase column is not of type double.");
                return c;
            }
        }
    }

    public class DataTable : DataTableBase
    {
        //List<IDataTableColumn> _columns;
        ParquetSchema _pSchema;
        object _lock = new object();

        public DataTable(ParquetSchema pSchema, int columnSize, TableType type, Instrument instrument, DataInterval interval) : base()
        {
            _pSchema = pSchema;
            //_columns = new List<IDataTableColumn>();
            Instrument = instrument;
            Interval = interval;
            TableType = type;

            createColumnsFromParquetSchema(columnSize);
        }

        public Instrument Instrument { get; init; }
        public DataInterval Interval { get; init; }

        public TableType TableType { get; init; }
        
        public ParquetSchema Schema { get { lock (_lock) { return _pSchema; } } }

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
                Columns.Clear();

                foreach (DataField field in _pSchema.DataFields)
                {
                    if (field.ClrType == typeof(DateTime))
                        Columns.Add(new DataTableColumn<DateTime>(field.Name, DataType.DateTime, columnSize));
                    else if (field.ClrType == typeof(double))
                        Columns.Add(new DataTableColumn<double>(field.Name, DataType.Double, columnSize));
                    else if (field.ClrType == typeof(long))
                        Columns.Add(new DataTableColumn<long>(field.Name, DataType.Int64, columnSize));
                    else if (field.ClrType == typeof(int))
                        Columns.Add(new DataTableColumn<int>(field.Name, DataType.Int32, columnSize));
                    else if (field.ClrType == typeof(byte))
                        Columns.Add(new DataTableColumn<byte>(field.Name, DataType.UInt8, columnSize));
                    else
                    {
                        Columns.Clear();
                        throw new Exception($"Unhandled parquet column type {field.ClrType.ToString()}");
                    }
                }
            }
        }

        public bool CompareColumnStructure(DataTable table)
        {
            lock (_lock)
            {
                if (table.Columns.Count != Columns.Count) return false;
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    IDataTableColumn c = table.Columns[i];
                    if (c.Name != Columns[i].Name) return false;
                    if (c.DataType != Columns[i].DataType) return false;
                }

                return true;
            }
        }

        public bool CompareColumnStructure(DataColumn[] columns)
        {
            lock (_lock)
            {
                if (columns.Length != Columns.Count) return false;
                for (int i = 0; i < columns.Length; i++)
                {
                    DataColumn c = columns[i];
                    if (c.Field.Name != Columns[i].Name) return false;
                    switch (Columns[i].DataType)
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
                    Columns[i].AddDataColumn(columns[i]);
                }
            }
        }

        public void AppendTable(DataTable table)
        {
            lock (_lock)
            {
                if (!CompareColumnStructure(table)) throw new ArgumentException();

                //TODO:verify alignment


                for (int i = 0; i < Columns.Count; i++)
                {
                    Columns[i].AddDataColumn(table.Columns[i]);
                }
            }
        }

        public DataTable DynamicSlice()
        {
            lock (_lock)
            {
                List<IDataTableColumn> newColumns = new List<IDataTableColumn>();

                DataTable sliceTable = new DataTable(Schema, 0,TableType, Instrument, Interval);

                for (int i = 0; i < Columns.Count; i++)
                {
                    IDataTableColumn srcColumn = Columns[i];
                    IDataTableColumn newCol = srcColumn.ExportDynamics();
                    newColumns.Add(newCol);
                }

                sliceTable.Columns = newColumns;
                return sliceTable;
            }
        }

        public DataTable Slice(int index, int length)
        {
            lock (_lock)
            {
                List<IDataTableColumn> newColumns = new List<IDataTableColumn>();
                DataTable sliceTable = new DataTable(Schema, 0, TableType, Instrument, Interval);

                for (int i = 0; i < Columns.Count; i++)
                {
                    IDataTableColumn srcColumn = Columns[i];
                    IDataTableColumn newCol = srcColumn.ExportRange(index, length);
                    newColumns.Add(newCol);
                }

                sliceTable.Columns = newColumns;
                return sliceTable;
            }
        }
    }
}
