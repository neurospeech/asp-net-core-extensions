using System;
using System.Data;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public class SqlColumn
    {

        public int Ordinal { get; set; }

        public string ColumnName { get; set; }

        public string[] OldNames { get; set; }

        public string ColumnDefault { get; set; }

        public decimal? NumericScale { get; set; }

        public decimal? NumericPrecision { get; set; }


        public string DataType { get; set; }

        public int DataLength { get; set; }

        public bool IsNullable { get; set; }

        public Type CLRType { get; set; }

        public DbType DbType { get; set; }

        public bool IsPrimaryKey { get; set; }
        public string ParamName
        {
            get
            {
                return "@P" + ColumnName.Replace(" ", "_");
            }
        }

        public string CopyFrom { get; internal set; }
        public bool IsIdentity { get; internal set; }

        public override bool Equals(object obj)
        {
            var dest = obj as SqlColumn;
            if (dest != null)
            {
                if (!DataType.Equals(dest.DataType, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (DataLength > 0 && dest.DataLength > 0)
                {
                    if (DataLength != dest.DataLength)
                        return false;
                }

                if (IsNullable != dest.IsNullable)
                    return false;

                //if (NumericPrecision != dest.NumericPrecision)
                //    return false;
                //if (NumericScale != NumericScale)
                //    return false;

                return true;
            }
            return base.Equals(obj);
        }

    }
}
