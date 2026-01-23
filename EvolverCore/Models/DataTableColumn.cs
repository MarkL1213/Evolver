using EvolverCore.Models.DataV2;
using System;
using System.Collections.Generic;
using System.Linq;
using Parquet.Data;


namespace EvolverCore.Models
{
    public enum DataType { Int32, Int64, UInt8, Double, DateTime };

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

        public void InitValues(List<object> values)
        {
            Array a = values.ToArray();
            _dataArrays.Add(a);
            
            RecalcOffsets();
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
            if (index == _cumulativeOffsets[i]) i++;

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

        public Array ToArray()
        {
            List<T> allData = new List<T>();

            for(int i=0;i< Count;i++)
            {
                allData.Add((T)GetValueAt(i));
            }

            return allData.ToArray();
        }

        public T this[int index]
        {
            get { return (T)GetValueAt(index); }
            internal set { SetValueAt(value, index); }
        }
    }
}
