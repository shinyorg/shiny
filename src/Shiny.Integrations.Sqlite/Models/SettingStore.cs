using System;
using SQLite;


namespace Shiny.Models
{
    public class SettingStore
    {
        [PrimaryKey]
        public string Key { get; set; }

        public bool? BoolValue { get; set; }
        public byte? ByteValue { get; set; }
        public byte[]? BytesValue { get; set; }
        public string? StringValue { get; set; }
        public ushort? UShortValue { get; set; }
        public short? ShortValue { get; set; }
        public uint? UIntValue { get; set; }
        public int? IntValue { get; set; }
        //public ulong? ULongValue { get; set; }
        public long? LongValue { get; set; }
        public float? FloatValue { get; set; }
        public double? DoubleValue { get; set; }
        public decimal? DecimalValue { get; set; }
        //public DateTime? DateTimeValue { get; set; }


        public object? GetValue()
        {
            if (this.StringValue != null)
                return this.StringValue;

            if (this.BoolValue != null)
                return this.BoolValue.Value;

            if (this.ByteValue != null)
                return this.ByteValue.Value;

            if (this.BytesValue != null)
                return this.BytesValue;

            if (this.UShortValue != null)
                return this.UShortValue.Value;

            if (this.ShortValue != null)
                return this.ShortValue.Value;

            if (this.UIntValue != null)
                return this.UIntValue.Value;

            if (this.IntValue != null)
                return this.IntValue.Value;

            //if (this.ULongValue != null)
            //    return this.ULongValue.Value;

            if (this.LongValue != null)
                return this.LongValue.Value;

            if (this.FloatValue != null)
                return this.FloatValue.Value;

            if (this.DoubleValue != null)
                return this.DoubleValue.Value;

            if (this.DecimalValue != null)
                return this.DecimalValue.Value;

            //if (this.DateTimeValue != null)
            //    return this.DateTimeValue.Value;

            return null;
        }


        public bool SetValue(object value)
        {
            this.Reset();
            if (value is ushort v1)
                this.UShortValue = v1;

            else if (value is short v2)
                this.ShortValue = v2;

            else if (value is uint v3)
                this.UIntValue = v3;

            else if (value is int v4)
                this.IntValue = v4;

            //else if (value is ulong v5)
            //    this.ULongValue = v5;

            else if (value is long v6)
                this.LongValue = v6;

            else if (value is float v7)
                this.FloatValue = v7;

            else if (value is double v8)
                this.DoubleValue = v8;

            else if (value is decimal v9)
                this.DecimalValue = v9;

            else if (value is bool v10)
                this.BoolValue = v10;

            else if (value is byte v11)
                this.ByteValue = v11;

            else if (value is byte[] v12)
                this.BytesValue = v12;

            else if (value is string v13)
                this.StringValue = v13;

            //else if (value is DateTime dt)
            //    this.DateTimeValue = dt;

            //else if (value is DateTimeOffset dto)
            //    this.DateTimeValue = dto.UtcDateTime;

            else
                return false;

            return true;
        }


        public void Reset()
        {
            this.StringValue = null;
            this.UShortValue = null;
            this.ShortValue = null;
            this.UIntValue = null;
            this.IntValue = null;
            this.LongValue = null;
            this.FloatValue = null;
            this.DoubleValue = null;
            this.DecimalValue = null;
            this.BoolValue = null;
            this.ByteValue = null;
            this.BytesValue = null;
            //this.DateTimeValue = null;
        }
    }
}
