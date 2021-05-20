using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NeuroSpeech.TemplatedQuery;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.EFCoreLiveMigration.PostGreSql
{
    public class PostGreSqlMigration : ModelMigrationBase
    {
        public PostGreSqlMigration(DbContext context) : base(context)
        {
        }

        public override string Escape(string name)
        {
            return $"\"{name.Trim('\"')}\"";
        }

        protected override void AddColumn(DbColumnInfo property)
        {
            Run($"ALTER TABLE {property.Table.EscapedNameWithSchema} ADD " + ToColumn(property));
        }        

        protected override void CreateIndex(SqlIndexEx index)
        {
            var name = index.Name;
            var tableName = GetTableNameWithSchema(index.DeclaringEntityType);
            var columns = string.Join(", ", index.Properties.Select(x => Escape(x.ColumnName())));
            string filter = index.Filter == null ? "" : " WHERE " + index.Filter;
            Run($"CREATE INDEX {name} ON {tableName} ({ columns }) {filter}");
        }

        protected override void DropIndex(SqlIndexEx index)
        {
            Run(TemplateQuery.New($"DROP INDEX IF EXISTS {Literal.DoubleQuoted(index.Name)}"));
        }

        protected override void CreateTable(DbTableInfo entity, List<DbColumnInfo> pkeys)
        {
            var tableName = entity.EscapedNameWithSchema;

            string createTable = $" CREATE TABLE {tableName} ({ string.Join(",", pkeys.Select(c => ToColumn(c))) }, " +
                 $"CONSTRAINT {entity.TableName}_pkey PRIMARY KEY( { string.Join(", ", pkeys.Select(x => x.EscapedColumnName)) } ))";

            Run(createTable);
        }

        protected override string GetTableNameWithSchema(IEntityType entity)
        {
            return Escape(entity.GetSchemaOrDefault()) + "." + Escape(entity.GetTableName());
        }

        private static string[] textTypes = new[] { "character varying", "varchar" };

        protected override bool IsText(string n) => textTypes.Any(a => a.Equals(n, StringComparison.OrdinalIgnoreCase));

        protected override bool IsDecimal(string n) => n.Equals("numeric", StringComparison.OrdinalIgnoreCase);

        protected override string LoadTableColumns(DbTableInfo table)
        {
            return Scripts.SqlServerGetSchema;
        }

        protected override void RenameColumn(DbColumnInfo property, string postFix)
        {
            var table = property.Table.EscapedNameWithSchema;
            var name = property.ColumnName;
            var newName = name + postFix;
            Run( $"ALTER TABLE {table} RENAME {Escape(name)} TO {Escape(newName)}");
        }

        protected override string ToColumn(DbColumnInfo c)
        {
            var name = $"{c.EscapedColumnName} {c.DataType}";

            if (c.DataLength != null)
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
            if (c.Precision != null)
            {
                var np = 18;
                var nps = 2;

                name += $"({ np },{ nps })";
            }
            if (!c.IsKey)
            {
                // lets allow nullable to every field...
                if (!c.IsNullable)
                {
                    name += " NOT NULL ";
                }
            }

            if (c.IsIdentity)
            {
                name += " GENERATED ALWAYS AS IDENTITY ";
            }

            if (!string.IsNullOrWhiteSpace(c.DefaultValue))
            {
                name += " DEFAULT " + c.DefaultValue;
            }
            return name;

        }

        protected override string LoadIndexes(IEntityType entity)
        {
            return NeuroSpeech.EFCoreLiveMigration.PostGreSql.Scripts.SqlServerGetIndexes;
        }
    }
}
