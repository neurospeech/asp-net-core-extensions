#nullable enable
using System;
using System.Collections.Generic;

namespace NeuroSpeech.EFCoreLiveMigration
{
    internal class MigrationEventList: IMigrationEvents
    {
        List<IMigrationEvents>? events;

        public void Add(IMigrationEvents handler)
        {
            this.events = this.events ?? new List<IMigrationEvents>();
            this.events.Add(handler);
        }

        public void OnColumnAdded(DbColumnInfo column, Column? existing = null)
        {
            if (this.events != null)
            {
                foreach (var e in this.events)
                {
                    e.OnColumnAdded(column, existing);
                }
            }
            Console.WriteLine($"Column {column.TableNameAndColumnName} Added.");
        }

        public void OnIndexCreated(SqlIndexEx index)
        {
            if (this.events != null)
            {
                foreach (var e in this.events)
                {
                    e.OnIndexCreated(index);
                }
            }
            Console.WriteLine($"Index {index.Name} Added.");
        }

        public void OnIndexDropped(SqlIndexEx index)
        {
            if (this.events != null)
            {
                foreach (var e in this.events)
                {
                    e.OnIndexDropped(index);
                }
            }
            Console.WriteLine($"Index {index.Name} Dropped.");
        }

        public void OnTableCreated(DbTableInfo table)
        {
            if (this.events != null)
            {
                foreach (var e in this.events)
                {
                    e.OnTableCreated(table);
                }
            }
            Console.WriteLine($"Table {table.EscapedNameWithSchema} Added.");
        }

        public void OnTableModified(
            DbTableInfo table,
            IReadOnlyCollection<DbColumnInfo> columnsAdded,
            IReadOnlyCollection<(Column from, DbColumnInfo to)> columnsRenamed,
            IReadOnlyCollection<(bool Dropped, SqlIndexEx index)> indexesUpdated)
        {
            if (this.events != null)
            {
                foreach (var e in this.events)
                {
                    e.OnTableModified(table, columnsAdded, columnsRenamed, indexesUpdated);
                }
            }
            Console.WriteLine($"Table {table.EscapedNameWithSchema} Sync Successful.");
        }
    }
}