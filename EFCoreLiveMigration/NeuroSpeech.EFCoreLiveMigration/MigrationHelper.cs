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
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NeuroSpeech.TemplatedQuery;

namespace NeuroSpeech.EFCoreLiveMigration
{

    public struct TableName
    {
        public readonly string Schema;
        public readonly string Name;
        public readonly Literal EscapedSchema;
        public readonly Literal EscapedName;
        public readonly Literal EscapedFullName;

        public TableName(string schema, string name, Func<string, string> escape)
        {
            this.Schema = schema;
            this.Name = name;
            this.EscapedSchema = new Literal( escape(schema));
            this.EscapedName = new Literal(escape(name));
            this.EscapedFullName = new Literal($"{escape(schema)}.{escape(name)}");
        }

        public override string ToString()
        {
            return EscapedFullName.Value;
        }

    }
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

            context.Database.Migrate();

            foreach (var entity in context.Model.GetEntityTypes())
            {
                // var relational = entity.Relational();

                if(entity.ClrType?.GetCustomAttribute<IgnoreMigrationAttribute>() != null)
                {
                    continue;
                }

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

                        // var mb = new MigrationBuilder(context.Database.ProviderName);
                        var schema = entity.GetSchema();
                        var table = new TableName(
                            string.IsNullOrWhiteSpace(schema) ? GetDefaultSchema() : schema, 
                            entity.GetTableName(), Escape);

                        SyncSchema(in table, columns);

                        SyncIndexes(in table, indexes);

                        SyncIndexes(in table, fkeys);

                        //var sp = (context as IInfrastructure<IServiceProvider>);

                        //var msg = sp.GetService<IMigrationsSqlGenerator>();

                        //var conn = sp.GetService<IRelationalConnection>();

                        //foreach(var cmd in msg.Generate(mb.Operations))
                        //{


                        //    cmd.ExecuteNonQuery(conn);
                        //}

                        tx.Commit();
                    }
                }
                finally {
                    context.Database.CloseConnection();
                }
            }

            

        }

        protected abstract void SyncIndexes(in TableName table, IEnumerable<IForeignKey> fkeys);
        protected abstract void SyncIndexes(in TableName table, IEnumerable<IIndex> indexes);

        protected abstract string GetDefaultSchema();

        protected virtual SqlColumn NewColumn() => new SqlColumn();

        private SqlColumn CreateColumn(IProperty x)
        {
            var r = NewColumn();

            r.CLRType = x.ClrType;
            r.ColumnDefault = x.GetDefaultValueSql();
            r.ColumnName = x.GetColumnName();
            r.DataLength = x.GetMaxLength() ?? 0;
            r.DataType = x.GetColumnTypeForSql();
            r.IsNullable = x.IsNullable;
            r.IsPrimaryKey = x.IsPrimaryKey();

            r.OldNames = x.GetOldNames();

            if (!x.IsNullable){
                r.ColumnDefault = x.GetDefaultValueSql() ?? (x.GetDefaultValue()?.ToString());
            }

            if (x.PropertyInfo
                ?.GetCustomAttribute<DatabaseGeneratedAttribute>()
                ?.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity) {
                r.IsIdentity = true;
            }

            return r;
        }

        public abstract string Escape(string name);

        public abstract DbCommand CreateCommand(String command, IEnumerable<KeyValuePair<string,object>> plist = null);

        public int Run(TemplateQuery query)
        {
            return context.ExecuteNonQuery(query);
        }

        public int Run(string command, Dictionary<string, object> plist = null)
        {
            using (var cmd = CreateCommand(command, plist))
            {
                try
                {
                    return cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"RunAsync failed for {command}", ex);
                }
            }
        }

        public SqlRowSet Read(TemplateQuery query)
        {
            var cmd = CreateCommand(query.Text, query.Values);
            return new SqlRowSet(cmd, cmd.ExecuteReader());
        }


        public SqlRowSet Read(string command, Dictionary<string, object> plist)
        {
            var cmd = CreateCommand(command, plist);
            return new SqlRowSet(cmd, cmd.ExecuteReader());
        }

        public abstract List<SqlColumn> GetCommonSchema(in TableName table);
        public abstract void SyncSchema(in TableName table, List<SqlColumn> schemaTable);

        public abstract List<SqlIndex> GetIndexes(in TableName table);


    }
}
