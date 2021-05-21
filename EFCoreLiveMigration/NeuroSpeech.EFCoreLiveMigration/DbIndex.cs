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
            if (Filter == null)
                return existingFilter == null;
            if (existingFilter == null)
                return false;
            if (Filter.Trim('(', ')').Trim().ToLower() == existingFilter.Trim('(',')').Trim().ToLower())
                return true;
            return false;
        }
    }
}
