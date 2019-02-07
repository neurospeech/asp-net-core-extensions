using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public abstract class MigrationHelper
    {
        protected readonly DbContext context;

        public DbTransaction Transaction { get; private set; }

        public MigrationHelper(DbContext context)
        {
            this.context = context;
        }

        public static MigrationHelper ForSqlServer(DbContext context) {
            return new SqlServerMigrationHelper(context);
        }

     

        public void Migrate() {

            context.Database.EnsureCreated();

            foreach (var entity in context.Model.GetEntityTypes())
            {
                var relational = entity.Relational();

                var columns = entity.GetProperties().Select(x => CreateColumn(x)).ToList();

                var indexes = entity.GetIndexes();

                var fkeys = entity.GetForeignKeys();

                var pk = columns.FirstOrDefault(x => x.IsPrimaryKey);

                if (pk == null)
                {
                    // do not generate for views
                    continue;
                }

                //if(entity.)

                


                try
                {
                        
                    context.Database.OpenConnection();
                    using (var tx = context.Database.GetDbConnection().BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        this.Transaction = tx;
                        SyncSchema(relational.Schema, relational.TableName, columns);

                        SyncIndexes(relational.Schema, relational.TableName, indexes);

                        SyncIndexes(relational.Schema, relational.TableName, fkeys);

                        tx.Commit();
                    }
                }
                finally {
                    context.Database.CloseConnection();
                }
            }

            

        }

        internal abstract void SyncIndexes(string schema, string tableName, IEnumerable<IForeignKey> fkeys);
        internal abstract void SyncIndexes(string schema, string tableName, IEnumerable<IIndex> indexes);

        private static SqlColumn CreateColumn(IProperty x)
        {
            var r = new SqlColumn
            {
                CLRType = x.ClrType,
                ColumnDefault = x.Relational().DefaultValueSql,
                ColumnName = x.Relational().ColumnName,
                DataLength = x.GetMaxLength() ?? 0,
                DataType = x.GetColumnType(),
                IsNullable = x.IsNullable,
                IsPrimaryKey = x.IsPrimaryKey()
            };

            var rr = x.Relational();


            r.OldNames = x.GetOldNames();

            if (!x.IsNullable){
                r.ColumnDefault = x.Relational().DefaultValueSql;
            }

            if (x.PropertyInfo.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity) {
                r.IsIdentity = true;
            }

            return r;
        }

        public abstract DbCommand CreateCommand(String command, Dictionary<string, object> plist = null);

        public int Run(string command, Dictionary<string, object> plist = null) {
            using (var cmd = CreateCommand(command, plist)) {
                try
                {
                    return cmd.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    throw new InvalidOperationException($"RunAsync failed for {command}", ex);
                }
            }
        }

        public SqlRowSet Read(string command, Dictionary<string, object> plist)
        {
            var cmd = CreateCommand(command, plist);
            return new SqlRowSet(cmd, cmd.ExecuteReader());
        }

        public abstract List<SqlColumn> GetCommonSchema(string name);
        public abstract void SyncSchema(string schema, string table, List<SqlColumn> schemaTable);

        public abstract List<SqlIndex> GetIndexes(string name);


    }
}
