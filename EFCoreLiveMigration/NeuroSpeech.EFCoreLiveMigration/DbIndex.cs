using System.Linq;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public class DbIndex
    {
        public string Name;
        internal string[] Columns;
        internal string Filter;

        internal bool IsSame(SqlIndexEx index, ModelMigrationBase migrationBase)
        {
            string thisColumns = string.Join(",", Columns);
            string indexColumns = string.Join(",", index.Properties.Select(x => migrationBase.Escape( x.ColumnName())));

            if (!thisColumns.EqualsIgnoreCase(indexColumns))
                return false;
            var existingFilter = index.GetFilter();
            return Filter == existingFilter;
        }
    }
}
