using Microsoft.EntityFrameworkCore;

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
