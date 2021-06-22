using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.EFCoreLiveMigration.PostGreSql
{
    class Scripts
    {
		public const string SqlServerGetSchema = @"select 
ISC.ordinal_position as Ordinal,
ISC.column_name as ColumnName,
ISC.column_default as ColumnDefault,
(ISC.is_nullable <> 'NO') as IsNullable,
ISC.data_type as DataType,
ISC.character_maximum_length as DataLength,
ISC.numeric_precision as NumericPrecision,
ISC.numeric_scale as NumericScale,
(ISC.is_identity = 'YES') as IsIdentity,
(SELECT 1 FROM information_schema.CONSTRAINT_COLUMN_USAGE as CCU
	WHERE CCU.column_name = ISC.column_name 
      AND CCU.table_name = ISC.table_name
      AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS as TC
    WHERE TC.constraint_name= CCU.constraint_name
	   AND TC.constraint_type = 'PRIMARY KEY')
) as IsPrimaryKey
from information_schema.columns as ISC 
where ISC.table_name = @TableName";

		public const string SqlServerGetIndexes = @"select
    t.relname as TableName,
    i.relname as IndexName,
	i.oid as IndexId,
    a.attname as ColumnName,
	a.atttypid as ColumnId
from
    pg_class t,
    pg_class i,
    pg_index ix,
    pg_attribute a
where
    t.oid = ix.indrelid
    and i.oid = ix.indexrelid
    and a.attrelid = t.oid
    and a.attnum = ANY(ix.indkey)
    and t.relkind = 'r'
    and t.relname = @TableName
order by
    t.relname,
    i.relname;";

	}
}
