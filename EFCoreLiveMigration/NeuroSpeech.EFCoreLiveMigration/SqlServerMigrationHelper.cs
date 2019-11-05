using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace NeuroSpeech.EFCoreLiveMigration
{
    internal class SqlServerMigrationHelper : MigrationHelper
    {
        public SqlServerMigrationHelper(DbContext context) : base(context)
        {
        }

        public override string Escape(string name) => $"[{name}]";

        public override DbCommand CreateCommand(string command, Dictionary<string, object> plist = null)
        {
            var cmd = context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = command;
            cmd.Transaction = Transaction;
            if (plist != null)
            {
                foreach (var p in plist)
                {
                    var px = cmd.CreateParameter();
                    px.ParameterName = p.Key;
                    px.Value = p.Value;                    
                    cmd.Parameters.Add(px);
                }
            }
            return cmd;
        }

        public override List<SqlColumn> GetCommonSchema(string name)
        {


            List<SqlColumn> columns = new List<SqlColumn>();
            string sqlColumns = Scripts.SqlServerGetSchema;

            using (var reader = Read(sqlColumns, new Dictionary<string, object> { { "@TableName", name } }))
            {


                while (reader.Read())
                {
                    SqlColumn col = new SqlColumn();

                    col.ColumnName = reader.GetValue<string>("ColumnName");
                    col.IsPrimaryKey = reader.GetValue<bool>("IsPrimaryKey");
                    col.IsNullable = reader.GetValue<string>("IsNullable") == "YES";
                    col.ColumnDefault = reader.GetValue<string>("ColumnDefault");
                    col.DataType = reader.GetValue<string>("DataType");
                    col.DataLength = reader.GetValue<int>("DataLength");
                    col.NumericPrecision = reader.GetValue<decimal?>("NumericPrecision");
                    col.NumericScale = reader.GetValue<decimal?>("NumericScale");
                    col.IsIdentity = reader.GetValue<bool>("IsIdentity");

                    columns.Add(col);
                }

            }
            return columns;
        }

        public override void SyncSchema(string schema, string name, List<SqlColumn> columns)
        {

            schema = string.IsNullOrWhiteSpace(schema) ? "dbo" : schema;

            var pkeys = columns.Where(x => x.IsPrimaryKey).ToList();
            
            string createTable = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{name}' AND TABLE_SCHEMA = '{schema}')"
                + $" CREATE TABLE {schema}.{name} ({ string.Join(",", pkeys.Select(c => ToColumn(c))) }, " +
                 $"PRIMARY KEY( { string.Join(", ", pkeys.Select(x=> this.Escape(x.ColumnName) )) } ))";

            Run(createTable);

            var destColumns = GetCommonSchema(name);

            List<SqlColumn> columnsToAdd = new List<SqlColumn>();

            foreach (var column in columns)
            {
                var dest = destColumns.FirstOrDefault(x => x.ColumnName == column.ColumnName);
                if (dest == null)
                {

                    // look for old names....
                    dest = destColumns.FirstOrDefault(x => column.OldNames != null && 
                        column.OldNames.Any( oc => oc.EqualsIgnoreCase(x.ColumnName) ));

                    if (dest == null)
                    {


                        columnsToAdd.Add(column);
                        continue;
                    }

                    Run($"EXEC sp_rename '{name}.{dest.ColumnName}', '{column.ColumnName}'");
                    dest.ColumnName = column.ColumnName;
                    
                }
                if (dest.Equals(column))
                    continue;


                columnsToAdd.Add(column);

                long m = DateTime.UtcNow.Ticks;

                column.CopyFrom = $"{dest.ColumnName}_{m}";

                Run($"EXEC sp_rename '{name}.{dest.ColumnName}', '{column.CopyFrom}'");

            }

            foreach (var column in columnsToAdd)
            {
                Run($"ALTER TABLE {name} ADD " + ToColumn(column));
            }

            Console.WriteLine($"Table {name} sync complete");


            var copies = columns.Where(x => x.CopyFrom != null).ToList();

            if (copies.Any()) {
                foreach (var copy in copies)
                {
                    var update = $"UPDATE {name} SET {this.Escape(copy.ColumnName)} = {this.Escape(copy.CopyFrom)};";
                    Run(update);
                }
            }
        }

        private static string[] textTypes = new[] { "nvarchar", "varchar" };

        private static bool IsText(string n) => textTypes.Any(a => a.Equals(n, StringComparison.OrdinalIgnoreCase));

        private static bool IsDecimal(string n) => n.Equals("decimal", StringComparison.OrdinalIgnoreCase);

        private string ToColumn(SqlColumn c)
        {
            var name = $"{this.Escape(c.ColumnName)} {c.DataType}";
            if (IsText(c.DataType))
            {
                if (c.DataLength > 0 && c.DataLength < int.MaxValue)
                {
                    name += $"({ c.DataLength })";
                }
                else
                {
                    name += "(MAX)";
                }
            }
            if (IsDecimal(c.DataType))
            {
                var np = c.NumericPrecision ?? 18;
                var nps = c.NumericScale ?? 2;

                name += $"({ np },{ nps })";
            }
            if (!c.IsPrimaryKey)
            {
                // lets allow nullable to every field...
                if (c.IsNullable)
                {
                    name += " NULL ";
                }
                else {
                    name += " NOT NULL ";
                }
            }
            else
            {
                //name += " PRIMARY KEY ";
                if (c.IsIdentity) {
                    name += " Identity ";
                }
            }
            return name;
        }

        internal override void SyncIndexes(string schema, string tableName, IEnumerable<IIndex> indexes)
        {


            var allIndexes = indexes.Select(x => new SqlIndex{
                Name = x.GetName(),
                Columns = x.Properties.Select(p => p.GetColumnName()).ToArray()
            });

            EnsureIndexes(tableName, allIndexes);


            
        }

        private void EnsureIndexes(string tableName, IEnumerable<SqlIndex> allIndexes)
        {
            var destIndexes = GetIndexes(tableName);
            foreach (var index in allIndexes)
            {

                var name = index.Name;
                var columns = index.Columns;

                var newColumns = columns.Select(x => $"{x} ASC").ToJoinString();

                var existing = destIndexes.FirstOrDefault(x => x.Name == name);
                if (existing != null)
                {
                    // see if all are ok...
                    var existingColumns = existing.Columns.ToJoinString();

                    if (existingColumns.EqualsIgnoreCase(newColumns))
                        continue;

                    // rename old index... 
                    var n = $"{name}_{System.DateTime.UtcNow.Ticks}";

                    Run($"EXEC sp_rename @FromName, @ToName, @Type", new Dictionary<string, object> {
                        { "@FromName", tableName + "." + name },
                        { "@ToName", n},
                        { "@Type", "INDEX" }
                    });
                }

                // lets create index...

                Run($"CREATE NONCLUSTERED INDEX {name} ON {tableName} ({ newColumns })");


            }
        }

        public override List<SqlIndex> GetIndexes(string name)
        {
            List<SqlIndex> list = new List<SqlIndex>();
            using (var reader = Read(Scripts.SqlServerGetIndexes, new Dictionary<string, object> {
                { "@TableName", name }
            })) {

                while (reader.Read()) {
                    var index = new SqlIndex();

                    index.Name = reader.GetValue<string>("IndexName");
                    index.Columns = new string[] {
                        reader.GetValue<string>("ColumnName")
                    };

                    list.Add(index);
                }

                list = list.GroupBy(x => x.Name).Select(x => new SqlIndex {
                    Name = x.Key,
                    Columns = x.SelectMany( c => c.Columns ).Select( c => $"[{c}]" ).ToArray()
                }).ToList();

            }
            return list;
        }

        internal override void SyncIndexes(string schema, string tableName, IEnumerable<IForeignKey> fkeys)
        {
            var allIndexes = fkeys.Select(x => new SqlIndex
            {
                Name = "IX" + x.GetConstraintName(),
                Columns = x.Properties.Select(p => p.GetColumnName()).ToArray()
            });

            EnsureIndexes(tableName, allIndexes);
        }
    }
}
