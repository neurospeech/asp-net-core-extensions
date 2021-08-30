#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public class DbTableInfo
    {
        public readonly string TableName;

        public readonly string Schema;

        public readonly string EscapedNameWithSchema;

        public readonly string EscapedTableName;

        public readonly string EscapedSchema;

        public readonly Type ClrType;

        public readonly IEntityType EntityType;

        internal readonly List<DbColumnInfo> columnsAdded = new List<DbColumnInfo>();
        internal readonly List<(Column from, DbColumnInfo to)> columnsRenamed = new List<(Column from, DbColumnInfo to)>();
        internal readonly List<(bool Dropped, SqlIndexEx Index)> indexedUpdated = new List<(bool Dropped, SqlIndexEx Index)>();

        public IReadOnlyCollection<DbColumnInfo> ColumnsAdded => columnsAdded.AsReadOnly();

        public IReadOnlyCollection<(Column from, DbColumnInfo to)> ColumnsRenamed => columnsRenamed.AsReadOnly();

        public IReadOnlyCollection<(bool Dropped, SqlIndexEx Index)> IndexesUpdated => indexedUpdated.AsReadOnly();

        public DbTableInfo(IEntityType type, Func<string, string> escape)
        {
            this.EntityType = type;
            this.ClrType = type.ClrType;
            this.TableName = type.GetTableName();
            this.EscapedTableName = escape(this.TableName);
            this.Schema = type.GetSchemaOrDefault();
            this.EscapedSchema = escape(this.Schema);
            this.EscapedNameWithSchema = $"{this.EscapedSchema}.{this.EscapedTableName}";
        }
    }
}