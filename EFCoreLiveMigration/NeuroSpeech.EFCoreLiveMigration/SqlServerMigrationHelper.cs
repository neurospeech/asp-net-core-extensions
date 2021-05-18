using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace NeuroSpeech.EFCoreLiveMigration
{

    public class PostGreSqlColumn: SqlColumn
    {

    }
    internal class SqlServerMigrationHelper : MigrationHelper
    {
        public SqlServerMigrationHelper(DbContext context) : base(context)
        {
        }

        protected override SqlColumn NewColumn() => new PostGreSqlColumn();

        public override string Escape(string name) => $"[{name}]";

        protected override string GetDefaultSchema() => "dbo";

        public override DbCommand CreateCommand(string command, IEnumerable<KeyValuePair<string, object>> plist = null)
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

        public override List<SqlColumn> GetCommonSchema(in TableName table)
        {


            List<SqlColumn> columns = new List<SqlColumn>();
            string sqlColumns = Scripts.SqlServerGetSchema;

            using (var reader = Read(sqlColumns, new Dictionary<string, object> { { "@TableName", table.Name } }))
            {


                while (reader.Read())
                {
                    SqlColumn col = NewColumn();

                    col.ColumnName = reader.GetValue<string>("ColumnName");
                    col.IsPrimaryKey = reader.GetValue<bool>("IsPrimaryKey");
                    col.IsNullable = reader.GetValue<bool>("IsNullable");
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

        public override void SyncSchema(in TableName table, List<SqlColumn> columns)
        {


            var pkeys = columns.Where(x => x.IsPrimaryKey).ToList();
            
            string createTable = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='{table.Name}' AND TABLE_SCHEMA = '{table.Schema}')"
                + $" CREATE TABLE {table.EscapedFullName} ({ string.Join(",", pkeys.Select(c => ToColumn(c))) }, " +
                 $"PRIMARY KEY( { string.Join(", ", pkeys.Select(x=> this.Escape(x.ColumnName) )) } ))";

            Run(createTable);

            var destColumns = GetCommonSchema(table);

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

                    Run($"EXEC sp_rename '{table.EscapedName}.{dest.ColumnName}', '{column.ColumnName}'");
                    dest.ColumnName = column.ColumnName;
                    
                }
                if (dest.Equals(column))
                    continue;


                columnsToAdd.Add(column);

                long m = DateTime.UtcNow.Ticks;

                column.CopyFrom = $"{dest.ColumnName}_{m}";

                Run($"EXEC sp_rename '{table.EscapedName}.{dest.ColumnName}', '{column.CopyFrom}'");

            }

            foreach (var column in columnsToAdd)
            {
                Run($"ALTER TABLE {table.EscapedName} ADD " + ToColumn(column));
            }

            Console.WriteLine($"Table {table} sync complete");


            var copies = columns.Where(x => x.CopyFrom != null).ToList();

            if (copies.Any()) {
                foreach (var copy in copies)
                {
                    var update = $"UPDATE {table.EscapedFullName} SET {this.Escape(copy.ColumnName)} = {this.Escape(copy.CopyFrom)};";
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
            if (!string.IsNullOrWhiteSpace(c.ColumnDefault))
            {
                name += " DEFAULT " + c.ColumnDefault;
            }
            return name;
        }

        protected override void SyncIndexes(in TableName table, IEnumerable<IIndex> indexes)
        {


            var allIndexes = indexes.Select(x => new SqlIndex{
                Name = x.GetName(),
                Columns = x.Properties.Select(p => p.GetColumnName()).ToArray()
            });

            EnsureIndexes(table, allIndexes);


            
        }

        private void EnsureIndexes(in TableName table, IEnumerable<SqlIndex> allIndexes)
        {
            var destIndexes = GetIndexes(table);
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
                        { "@FromName", table.EscapedName + "." + name },
                        { "@ToName", n},
                        { "@Type", "INDEX" }
                    });
                }

                // lets create index...

                Run($"CREATE NONCLUSTERED INDEX {name} ON {table.EscapedFullName} ({ newColumns })");


            }
        }

        public override List<SqlIndex> GetIndexes(in TableName table)
        {
            List<SqlIndex> list = new List<SqlIndex>();
            using (var reader = Read(Scripts.SqlServerGetIndexes, new Dictionary<string, object> {
                { "@TableName", table.Name }
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

        protected override void SyncIndexes(in TableName table, IEnumerable<IForeignKey> fkeys)
        {
            var allIndexes = fkeys.Select(x => new SqlIndex
            {
                Name = "IX" + x.GetConstraintName(),
                Columns = x.Properties.Select(p => p.GetColumnName()).ToArray()
            });

            EnsureIndexes(table, allIndexes);
        }
    }
}
