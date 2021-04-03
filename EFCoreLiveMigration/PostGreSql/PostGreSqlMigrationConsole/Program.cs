using LiveMigrationConsole.Models;
using Microsoft.EntityFrameworkCore;
using NeuroSpeech.EFCoreLiveMigration;

namespace LiveMigrationConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            DbContextOptionsBuilder<ERPContext> options = new DbContextOptionsBuilder<ERPContext>();

            Npgsql.NpgsqlConnectionStringBuilder sb = new Npgsql.NpgsqlConnectionStringBuilder();
            sb.Host = "localhost";
            sb.Database = "castyy";
            sb.Username = "postgres";
            sb.Password = "abcd123";

            options.UseNpgsql(sb.ConnectionString);

            using (var db = new ERPContext(options.Options)) {

                db.PostGreSqlMigrationHelper().Migrate();
                
            }

        }
    }
}
