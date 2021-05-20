#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.ComponentModel;
using System.Reflection;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public class DbColumnInfo
    {
        public readonly DbTableInfo Table;
        public readonly IProperty Property;
        public readonly string ColumnName;
        public readonly string EscapedColumnName;
        public readonly string TableNameAndColumnName;
        public readonly string EscapedTableNameAndColumnName;
        public readonly string DataType;
        public readonly int? DataLength;
        public readonly int? Precision;
        public readonly int? DecimalScale;
        public readonly bool IsKey;
        public readonly bool IsIdentity;
        public readonly bool IsNullable;
        public readonly string? DefaultValue;

        public DbColumnInfo(
            DbTableInfo table, 
            IProperty property, 
            Func<string,string> escape)
        {
            this.Table = table;
            this.Property = property;
            this.ColumnName = property.ColumnName();
            this.TableNameAndColumnName = Table.TableName + "." + ColumnName;
            this.EscapedColumnName = escape(this.ColumnName);
            this.EscapedTableNameAndColumnName = table.EscapedTableName + "." + this.EscapedColumnName;

            var (length, d) = property.GetColumnDataLength();

            this.DataType = property.GetColumnTypeForSql();
            if (property.ClrType.IsAssignableFrom(typeof(decimal)))
            {
                var ps = property.GetScale();
                var pp = property.GetPrecision();
                this.Precision = pp ?? length;
                this.DecimalScale = ps ?? d;
            }
            else
            {
                this.DataLength = length
                    ?? (property.ClrType == typeof(string) ? (int?)int.MaxValue : null);
            }
            this.IsKey = property.IsKey();
            this.IsIdentity = property.IsIdentityColumn(table.EntityType);
            this.IsNullable = property.IsColumnNullable();
            this.DefaultValue = property.GetDefaultValueSql();

            if(this.DefaultValue == null)
            {
                var dv =  property.PropertyInfo?.GetCustomAttribute<DefaultValueAttribute>();
                if(dv?.Value != null)
                {
                    if (dv.Value is string sv)
                    {
                        this.DefaultValue = sv;
                    } else
                    {
                        throw new InvalidOperationException($"Default value must be provided in string literal equivalent in SQL");
                    }
                }
            }
        }
    }
}