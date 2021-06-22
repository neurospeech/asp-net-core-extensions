#nullable enable
using System.Collections.Generic;

namespace NeuroSpeech.EFCoreLiveMigration
{

    public abstract class MigrationEvents<T> : IMigrationEvents
    {
        void IMigrationEvents.OnColumnAdded(DbColumnInfo column, Column? existing)
        {
            if(column.Table.ClrType == typeof(T))
            {
                OnColumnAdded(column, existing);
            }
        }


        void IMigrationEvents.OnIndexCreated(SqlIndexEx index)
        {
            if (index.Table.ClrType == typeof(T))
            {
                OnIndexCreated(index);
            }
        }


        void IMigrationEvents.OnIndexDropped(SqlIndexEx index)
        {
            if (index.Table.ClrType == typeof(T))
            {
                OnIndexDropped(index);
            }
        }


        void IMigrationEvents.OnTableCreated(DbTableInfo table)
        {
            if (table.ClrType == typeof(T))
            {
                OnTableCreated(table);
            }
        }


        void IMigrationEvents.OnTableModified(DbTableInfo table, IReadOnlyCollection<DbColumnInfo> columnsAdded, IReadOnlyCollection<(Column from, DbColumnInfo to)> columnsRenamed, IReadOnlyCollection<(bool Dropped, SqlIndexEx index)> indexesUpdated)
        {
            if (table.ClrType == typeof(T))
            {
                OnTableModified(table, columnsAdded, columnsRenamed, indexesUpdated);
            }
        }

        protected abstract void OnIndexCreated(SqlIndexEx index);
        protected abstract void OnIndexDropped(SqlIndexEx index);
        protected abstract void OnTableCreated(DbTableInfo table);
        protected abstract void OnColumnAdded(DbColumnInfo column, Column? existing);
        protected abstract void OnTableModified(DbTableInfo table, IReadOnlyCollection<DbColumnInfo> columnsAdded, IReadOnlyCollection<(Column from, DbColumnInfo to)> columnsRenamed, IReadOnlyCollection<(bool Dropped, SqlIndexEx index)> indexesUpdated);
    }

}