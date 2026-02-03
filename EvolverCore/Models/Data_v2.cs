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

namespace EvolverCore.Models
{
    public enum TickType : byte { Bid, Ask };

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
        public Bar(DateTime time, double open, double high, double low, double close, double bid, double ask, long volume)
        {
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Bid = bid;
            Ask = ask;
            Volume = volume;
        }

        public DateTime Time { get; init; }
        public double Open { get; init; }
        public double High { get; init; }
        public double Low { get; init; }
        public double Close { get; init; }
        public double Bid { get; init; }
        public double Ask { get; init; }
        public long Volume { get; init; }

        public static ParquetSchema GetSchema()
        {
            List<DataField> fields = new List<DataField>();

            fields.Add(new DataField("Time", typeof(DateTime)));
            fields.Add(new DataField("Open", typeof(double)));
            fields.Add(new DataField("High", typeof(double)));
            fields.Add(new DataField("Low", typeof(double)));
            fields.Add(new DataField("Close", typeof(double)));
            fields.Add(new DataField("Bid", typeof(double)));
            fields.Add(new DataField("Ask", typeof(double)));
            fields.Add(new DataField("Volume", typeof(long)));

             return new ParquetSchema(fields);
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
        //public static BarTable ConvertSeriesToBarTable(InstrumentDataSeries series)
        //{
        //    if (series.Instrument == null)
        //        throw new ArgumentException("Series Instrument can not be null.");

        //    ParquetSchema barSchema = Bar.GetSchema();
        //    DataTable table = new DataTable(barSchema, series.Count, TableType.Bar, series.Instrument, series.Interval);

        //    DataTableColumn<DateTime> timeCol = table.Column("Time") as DataTableColumn<DateTime> ?? throw new NullReferenceException();
        //    DataTableColumn<double> openCol = table.Column("Open") as DataTableColumn<double> ?? throw new NullReferenceException();
        //    DataTableColumn<double> highCol = table.Column("High") as DataTableColumn<double> ?? throw new NullReferenceException();
        //    DataTableColumn<double> lowCol = table.Column("Low") as DataTableColumn<double> ?? throw new NullReferenceException();
        //    DataTableColumn<double> closeCol = table.Column("Close") as DataTableColumn<double> ?? throw new NullReferenceException();
        //    DataTableColumn<long> volumeCol = table.Column("Volume") as DataTableColumn<long> ?? throw new NullReferenceException();

        //    for (int i = 0; i < series.Count; i++)
        //    {
        //        timeCol[i] = series[i].Time;
        //        openCol[i] = series[i].Open;
        //        highCol[i] = series[i].High;
        //        lowCol[i] = series[i].Low;
        //        closeCol[i] = series[i].Close;
        //        volumeCol[i] = series[i].Volume;
        //    }

        //    return new BarTable(table);
        //}

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
