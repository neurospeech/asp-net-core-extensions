using Microsoft.EntityFrameworkCore;
using NeuroSpeech.EFCoreLiveMigration.PostGreSql;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public static class MigrationHelperExtensions
    {
        public static MigrationHelper PostGreSqlMigrationHelper(this DbContext context)
        {
            return new PostGreSqlMigrationHelper(context);
        }
    }
}
