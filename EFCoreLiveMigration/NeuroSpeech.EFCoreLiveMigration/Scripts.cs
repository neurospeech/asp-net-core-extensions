using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroSpeech.EFCoreLiveMigration
{
	class Scripts
	{

		public const string SqlServerGetSchema = @"SELECT 
			IC.ORDINAL_POSITION as Ordinal,
			IC.COLUMN_NAME as ColumnName, 
			IC.COLUMN_DEFAULT as ColumnDefault, 
			(CASE IC.IS_NULLABLE WHEN 'YES' THEN 1 ELSE 0 END) as IsNullable, 
			IC.DATA_TYPE as DataType,
			IC.CHARACTER_MAXIMUM_LENGTH as DataLength,
			IC.NUMERIC_PRECISION as NumericPrecision, 
			IC.NUMERIC_SCALE as NumericScale, 
			COLUMNPROPERTY(object_id(TABLE_SCHEMA+'.'+TABLE_NAME), COLUMN_NAME, 'IsIdentity') as IsIdentity,
			(SELECT 1 FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE as CCU WHERE 
				CCU.COLUMN_NAME = IC.COLUMN_NAME AND 
				CCU.TABLE_NAME = IC.TABLE_NAME AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS as TC WHERE
					TC.CONSTRAINT_NAME = CCU.CONSTRAINT_NAME AND
					TC.TABLE_NAME=IC.TABLE_NAME AND TC.CONSTRAINT_TYPE='PRIMARY KEY') ) as IsPrimaryKey 
			FROM INFORMATION_SCHEMA.COLUMNS AS IC WHERE IC.TABLE_NAME=@TableName AND IC.TABLE_SCHEMA=@SchemaName;";


		public const string SqlServerGetIndexes = @"SELECT 
					t.name as TableName,
					ind.name as IndexName,
					ind.index_id as IndexId,
					ic.index_column_id as ColumnId,
					col.name as ColumnName,
					ind.filter_definition as [Filter]
			FROM 
					sys.indexes ind 
			INNER JOIN 
					sys.index_columns ic ON  ind.object_id = ic.object_id and ind.index_id = ic.index_id 
			INNER JOIN 
					sys.columns col ON ic.object_id = col.object_id and ic.column_id = col.column_id 
			INNER JOIN 
					sys.tables t ON ind.object_id = t.object_id 
			INNER JOIN
					sys.schemas s ON t.schema_id = s.schema_id
			WHERE 
					ind.is_primary_key = 0 
					AND ind.is_unique = 0 
					AND ind.is_unique_constraint = 0 
					AND t.is_ms_shipped = 0 
					AND t.name = @TableName
					AND s.name = @SchemaName
			ORDER BY 
					t.name, ind.name, ind.index_id, ic.index_column_id;";
	}
}
