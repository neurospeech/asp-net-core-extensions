using Microsoft.EntityFrameworkCore;
using NeuroSpeech.EFCoreLiveMigration.PostGreSql;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public static class MigrationHelperExtensions
    {
        public static ModelMigrationBase ForPostGreSqlMigration(this DbContext context)
        {
            return new PostGreSqlMigration(context);
        }
    }
}
