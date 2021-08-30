﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Text;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public class Column
    {
        public string ColumnName;
        public bool IsPrimaryKey;
        public bool IsNullable;
        public string ColumnDefault;
        public string DataType;
        public int DataLength;
        public decimal? NumericPrecision;
        public decimal? NumericScale;
        public bool IsIdentity;

        internal bool IsSame(DbColumnInfo column)
        {
            var x = column.Property;
            if (!x.GetColumnTypeForSql().EqualsIgnoreCase(this.DataType))
                return false;
            int xLength = x.GetMaxLength() ?? 0;
            if (DataLength > 0 && xLength > 0)
            {
                if (DataLength != xLength)
                {
                    // smaller value is fine...
                    if(DataLength < xLength)
                        return false;
                }
            }

            if (IsNullable != x.IsColumnNullable())
                return false;

            //var xColumnDefault = x.GetDefaultValueSql() ?? x.GetDefaultValue()?.ToString();
            //if (!x.IsColumnNullable())
            //{
            //    if (!(string.IsNullOrWhiteSpace(ColumnDefault)
            //        && string.IsNullOrWhiteSpace(xColumnDefault)))
            //    {
            //        return ColumnDefault == xColumnDefault;
            //    }
            //}
            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(DataType);
            if(DataLength > 0)
            {
                sb.Append('(');
                sb.Append(DataLength);
                sb.Append(')');
            }
            if(NumericPrecision != null)
            {
                sb.Append('(');
                sb.Append(NumericPrecision);
                sb.Append(',');
                sb.Append(NumericScale);
                sb.Append(')');
            }
            if (IsNullable)
            {
                sb.Append(" NULL ");
            } else
            {
                sb.Append(" NOT NULL ");
            }
            return sb.ToString();
        }
    }
}
