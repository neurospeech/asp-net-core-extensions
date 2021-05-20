#nullable enable
using System.Collections.Generic;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public interface IMigrationEvents
    {

        void OnTableCreated(DbTableInfo table);

        void OnColumnAdded(DbColumnInfo column, Column? existing);

        void OnIndexDropped(SqlIndexEx index);

        void OnIndexCreated(SqlIndexEx index);

        void OnTableModified(
            DbTableInfo table,
            IReadOnlyCollection<DbColumnInfo> columnsAdded,
            IReadOnlyCollection<(Column from, DbColumnInfo to)> columnsRenamed,
            IReadOnlyCollection<(bool Dropped, SqlIndexEx index)> indexesUpdated);

    }

}