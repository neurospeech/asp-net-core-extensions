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

        protected override void AddColumn(IProperty property)
        {
            var tableName = GetTableNameWithSchema(property.DeclaringEntityType);
            Run($"ALTER TABLE {tableName} ADD " + ToColumn(property));
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

        protected override void EnsureCreated(IEntityType entity)
        {
            var pkeys = entity.GetProperties()
                .Where(x => x.IsPrimaryKey())
                .ToList();

            var tableName = GetTableNameWithSchema(entity);

            string createTable = $" CREATE TABLE IF NOT EXISTS {tableName} ({ string.Join(",", pkeys.Select(c => ToColumn(c))) }, " +
                 $"CONSTRAINT {entity.GetTableName()}_pkey PRIMARY KEY( { string.Join(", ", pkeys.Select(x => this.Escape(x.ColumnName()))) } ))";

            Run(createTable);
        }

        protected override string GetTableNameWithSchema(IEntityType entity)
        {
            return Escape(entity.GetSchemaOrDefault()) + "." + Escape(entity.GetTableName());
        }

        private static string[] textTypes = new[] { "character varying", "varchar" };

        protected override bool IsText(string n) => textTypes.Any(a => a.Equals(n, StringComparison.OrdinalIgnoreCase));

        protected override bool IsDecimal(string n) => n.Equals("numeric", StringComparison.OrdinalIgnoreCase);

        protected override string LoadTableColumns(IEntityType declaringEntityType)
        {
            return Scripts.SqlServerGetSchema;
        }

        protected override void RenameColumn(IProperty property, string postFix)
        {
            var table = GetTableNameWithSchema(property.DeclaringEntityType);
            var name = property.GetColumnName();
            var newName = name + postFix;
            Run( $"ALTER TABLE {table} RENAME {Escape(name)} TO {Escape(newName)}");
        }

        protected override string ToColumn(IProperty c)
        {
            var columnName = c.ColumnName();

            var dataType = c.GetColumnTypeForSql();

            var dataLength = c.GetMaxLength() ?? 0;

            var name = $"{this.Escape(columnName)} {dataType}";

            if (IsText(dataType))
            {
                if (dataLength > 0 && dataLength < int.MaxValue)
                {
                    name += $"({ dataLength })";
                }
                else
                {
                    name += "(MAX)";
                }
            }
            if (IsDecimal(dataType))
            {
                var np = 18;
                var nps = 2;

                name += $"({ np },{ nps })";
            }
            if (!c.IsKey())
            {
                // lets allow nullable to every field...
                if (!c.IsColumnNullable())
                {
                    name += " NOT NULL ";
                }
            }

            var isIdentity = c.PropertyInfo
                ?.GetCustomAttribute<DatabaseGeneratedAttribute>()
                ?.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity;

            if (isIdentity)
            {
                name += " GENERATED ALWAYS AS IDENTITY ";
            }

            if (!string.IsNullOrWhiteSpace(c.GetDefaultValueSql()))
            {
                name += " DEFAULT " + c.GetDefaultValueSql();
            }
            return name;

        }

        protected override string LoadIndexes(IEntityType entity)
        {
            return NeuroSpeech.EFCoreLiveMigration.PostGreSql.Scripts.SqlServerGetIndexes;
        }
    }
}
