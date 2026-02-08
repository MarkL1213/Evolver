using Parquet.Data;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;


namespace EvolverCore.Models
{
    public enum DataType { Int32, Int64, UInt8, Double, DateTime };



    public class ColumnPointer<T> where T : struct
    {
        private DataTableColumn<T>? _column;
        private BarTablePointer? _parentTable;

        int _startOffset = -1;
        int _endOffset = -1;


        internal int StartOffset { get { return _parentTable != null ? _parentTable.StartOffset : _startOffset; } }
        internal int EndOffset { get { return _parentTable != null ? _parentTable.EndOffset : _endOffset; } }

        internal ColumnPointer(DataTableColumn<T>? column, int startOffset, int endOffset)
        {
        }

        internal ColumnPointer(BarTablePointer? parentTable, DataTableColumn<T>? column=null)
        {
            _column = column;
            _parentTable = parentTable;
        }

        public T GetValueAt(int index)=> (T)_column!.GetValueAt(StartOffset + index);

        public int RawFindIndex(T item)
        {
            DataTableColumn<T>? dataColumn = _column as DataTableColumn<T>;
            if (dataColumn == null) return -1;

            return dataColumn.FindIndex(item);
        }

        public int FindIndex(T item)
        {
            DataTableColumn<T>? dataColumn = _column as DataTableColumn<T>;
            if (dataColumn == null) return -1;

            return dataColumn.FindIndex(item, StartOffset, EndOffset);
        }

        public int GetNearestIndex(T item)
        {
            DataTableColumn<T>? dataColumn = _column as DataTableColumn<T>;
            if (dataColumn == null) return -1;

            return dataColumn.GetNearestIndex(item, StartOffset, EndOffset) - StartOffset;
        }

        public int Count
        {
            get
            {
                return _parentTable != null ? _parentTable.RowCount : (_startOffset == _endOffset ? 0 : _endOffset - _startOffset + 1);
            }
        }

        public T Min()
        {
            DataTableColumn<T>? col = _column as DataTableColumn<T>;
            if (col == null) throw new EvolverException("DataTableColumn is invalid type.");
            
            return col.Min(StartOffset, EndOffset);
        }

        public T Max()
        {
            DataTableColumn<T>? col = _column as DataTableColumn<T>;
            if (col == null) throw new EvolverException("DataTableColumn is invalid type.");

            return col.Max(StartOffset, EndOffset);

        }

        public T this[int barsAgo]
        {
            get
            {
                //FIXME : this is going to become a problem for output tables. need a way to keep current that doesn't link to a parent bar table
                return GetValueAt(_parentTable!.CurrentBar - barsAgo);
            }
        }
    }

    public interface IDataTableColumn
    {
        public DataType DataType { get; }
        public int Count { get; }

        public int[] Offsets { get; }

        public List<Array> Data { get; }
        public string Name { get; }
        public Array Series { get; }

        //public object GetValueAt(int index);

        public Array ToArray();

        public IDataTableColumn ExportRange(int index, int length);
        public IDataTableColumn ExportDynamics();
        public void AddDataColumn(DataColumn column);
        public void AddDataColumn(IDataTableColumn column);

        //public void SetValues(List<object> values);
    }

    public static class DataTableColumnFactory
    {
        public static DataTableColumn<T> CopyBlankTableColumn<T>(IDataTableColumn sourceColumn) where T : struct
        {
            DataTableColumn<T> c = new DataTableColumn<T>(sourceColumn.Name, sourceColumn.DataType, sourceColumn.Count);
            return c;
        }
    }



    public class DataTableColumn<T> : IDataTableColumn where T : struct
    {
        public DataTableColumn(string name, DataType dataType, int columnSize)
        {
            Name = name;
            DataType = dataType;
            _series = new List<T>(columnSize);
        }

        List<Array> _dataArrays = new List<Array>();
        int[] _cumulativeOffsets = new int[1] { 0 };
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

            _dataArrays.Add(column.Data);

            RecalcOffsets();
        }

        void RecalcOffsets()
        {
            List<int> offsetList = new List<int>();
            int n = 0;
            for (int i = 0; i < _dataArrays.Count;i++) {
                n = n + _dataArrays[i].Length;
                offsetList.Add(n);
            }

            n = n + _series.Count;
            offsetList.Add(n);

            _cumulativeOffsets = offsetList.ToArray();
        }

        public void AddDataColumn(IDataTableColumn column)
        {
            if (_series.Count > 0) throw new Exception("Target series can not have an un-serialized data chunk.");

            _dataArrays.AddRange(column.Data);
            _series.AddRange((T[])column.Series);

            RecalcOffsets();
        }

        public void InitValues(List<T> values)
        {
            Array a = values.ToArray();
            _dataArrays.Add(a);
            
            RecalcOffsets();
        }

        public IDataTableColumn ExportRange(int index, int length)
        {
            DataTableColumn<T> newCol = DataTableColumnFactory.CopyBlankTableColumn<T>(this);

            List<T> values = new List<T>();
            for (int i = index; i < index + length; i++)
                values.Add(GetValueAt(i));

            newCol.SetValues(values);
            return newCol;
        }

        public T Min(int startIndex, int endIndex)
        {
            T min = default(T);
            bool first = false;
            for (int i = startIndex; i <= endIndex; i++)
            {
                T v = (T)GetValueAt(i);
                IComparable? c = v as IComparable;
                if (c == null) throw new EvolverException("Value type is not comparable.");

                if (c.CompareTo(min) < 0 || !first)
                { min = v; first = true; }
            }
            return min;
        }
        public T Max(int startIndex, int endIndex)
        {
            T max = default(T);
            bool first = false;
            for (int i = startIndex; i <= endIndex; i++)
            {
                T v = (T)GetValueAt(i);
                IComparable? c = v as IComparable;
                if (c == null) throw new EvolverException("Value type is not comparable.");

                if (c.CompareTo(max) > 0 || !first)
                { max = v; first = true; }
            }
            return max;
        }

        public int GetNearestIndex(T item, int startIndex, int endIndex)
        {
            double distance = double.MaxValue;
            int index = -1;

            for (int i = startIndex; i <= endIndex; i++)
            {
                double valueDistance;
                switch (DataType)
                {
                    case DataType.Double:
                        double dv = (double)(object)GetValueAt(i);
                        double dn = (double)(object)item;
                        valueDistance = Math.Abs(dv - dn);
                        break;
                    case DataType.UInt8:
                    case DataType.Int32:
                    case DataType.Int64:
                        long lv = (long)(object)GetValueAt(i);
                        long ln = (long)(object)item;
                        valueDistance = Math.Abs(lv - ln);
                        break;
                    case DataType.DateTime:
                        long tv = ((DateTime)(object)GetValueAt(i)).Ticks;
                        long tn = ((DateTime)(object)item).Ticks;
                        valueDistance = Math.Abs(tv - tn);
                        break;
                    default:
                        throw new EvolverException("Invalid data type.");
                }

                if (valueDistance < distance)
                {
                    distance = valueDistance;
                    index = i;
                }
                
            }

            return index;
        }

        public IDataTableColumn ExportDynamics()
        {
            DataTableColumn<T> newCol = DataTableColumnFactory.CopyBlankTableColumn<T>(this);

            List<T> values = new List<T>();
            foreach (T t in _series)
                values.Add(t);

            newCol.SetValues(values);
            return newCol;
        }

        public void SetValues(List<T> values)
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


        public T GetValueAt(int index)
        {
            //determine where index points based on offsets
            //return offset shifted index from correct array/list
            int i = Array.BinarySearch(_cumulativeOffsets, index);
            if (i < 0) i = ~i;
            if (index == _cumulativeOffsets[i]) i++;

            if (i == _cumulativeOffsets.Length - 1)
                return i >= 1 ? _series[index - _cumulativeOffsets[i - 1]]! : _series[index]!;
            else
            {
                Array a = _dataArrays[i];
                    return i >= 1 ? (T)a.GetValue(index - _cumulativeOffsets[i - 1])! : (T)a.GetValue(index)!;
            }
        }

        public void SetValueAt(T value, int index)
        {
            int i = Array.BinarySearch(_cumulativeOffsets, index);
            if (i < 0) i = ~i;

            if (i == _cumulativeOffsets.Length - 1)
            {
                int n = i>= 1 ? index - _cumulativeOffsets[i - 1] : index;
                if (n == _series.Count)
                {
                    _series.Add(value);
                    RecalcOffsets();
                }
                else _series[n] = value;
            }
            else
            {
                Array a = _dataArrays[i];
                if (i >= 1) a.SetValue(value, index - _cumulativeOffsets[i - 1]);
                else a.SetValue(value, index);
            }
        }

        public void AddValue(T value)
        {
            _series.Add(value);
        }

        public Array ToArray()
        {
            List<T> allData = new List<T>();

            for (int i = 0; i < Count; i++) { allData.Add((T)GetValueAt(i)); }

            return allData.ToArray();
        }

        public int FindIndex(T item, int start, int end) { return FindIndexRecursive(item, start, end); }

        public int FindIndex(T item) { return FindIndexRecursive(item, 0, RowCount()); }

        private int FindIndexRecursive(T item, int start, int end)
        {
            if (start > end) return -1;

            int mid = (end - start) / 2 + start;
            T value = GetValueAt(mid);
            switch (DataType)
            {
                case DataType.Double:
                    if ((double)(object)value! == (double)(object)item!) return mid;
                    break;
                case DataType.DateTime:
                    if ((DateTime)(object)value! == (DateTime)(object)item!) return mid;
                    break;
                default:
                    throw new EvolverException("Unknown data type in FindIndex.");
            }
            
            
            IComparable? itemComparable = item as IComparable;
            if(itemComparable == null) return -1;

            if (itemComparable.CompareTo(value) < 0) return FindIndexRecursive(item, start, mid - 1);
            else return FindIndexRecursive(item, mid + 1, end);
        }

        public T this[int barsAgo]
        {
            get { return (T)GetValueAt(Count - 1 - barsAgo); }
            internal set { SetValueAt(value, Count - 1 - barsAgo); }
        }
    }
}
