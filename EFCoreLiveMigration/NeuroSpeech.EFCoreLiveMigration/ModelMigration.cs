using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NeuroSpeech.TemplatedQuery;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public class ModelMigration : ModelMigrationBase
    {

        public ModelMigration(DbContext context) : base(context)
        {

        }


        internal protected override string LoadTableColumns(IEntityType declaringEntityType)
        {
            return Scripts.SqlServerGetSchema;
        }

        internal protected override string LoadIndexes(IEntityType entity)
        {
            return Scripts.SqlServerGetIndexes;
        }

        public override string Escape(string name)
        {
            return $"[{name}]";
        }

        protected override void AddColumn(IProperty property)
        {
            Run($"ALTER TABLE {GetTableNameWithSchema(property.DeclaringEntityType)} ADD " 
                + ToColumn(property));
        }

        protected override void RenameColumn(IProperty property, string postFix)
        {
            Run($"EXEC sp_rename '{GetTableNameWithSchema(property.DeclaringEntityType)}.{property.ColumnName()}', '{property.ColumnName()}{postFix}'");
        }

        protected override void EnsureCreated(IEntityType entity)
        {
            var pkeys = entity
                .GetProperties()
                .Where(x => x.IsKey())
                .ToList();

            var tableName = entity.GetTableName();
            var schema = entity.GetSchemaOrDefault();
            var existsCheck = TemplateQuery.New($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME={tableName} AND TABLE_SCHEMA={schema}");

            var createTable = TemplateQuery.Literal(@$"
                    CREATE TABLE {GetTableNameWithSchema(entity)} ({ string.Join(",", pkeys.Select(c => ToColumn(c, entity))) }, 
                    PRIMARY KEY( { string.Join(", ", pkeys.Select(x => this.Escape(x.ColumnName()))) } ))");

            var finalQuery = TemplateQuery.New( $"IF NOT EXISTS ({existsCheck}) {createTable}");

            Run(finalQuery);
        }


        private static string[] textTypes = new[] { "nvarchar", "varchar" };

        protected override bool IsText(string n) => textTypes.Any(a => a.Equals(n, StringComparison.OrdinalIgnoreCase));

        protected override bool IsDecimal(string n) => n.Equals("decimal", StringComparison.OrdinalIgnoreCase);

        protected override string GetTableNameWithSchema(IEntityType entity)
        {
            return $"{Escape(entity.GetSchemaOrDefault())}.{Escape(entity.GetTableName())}";
        }

        protected override string ToColumn(IProperty c, IEntityType entity = null)
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
                if (c.IsColumnNullable())
                {
                    name += " NULL ";
                }
                else
                {
                    name += " NOT NULL ";
                }
            }

            var isIdentity = c.DeclaringEntityType == entity && c.GetValueGenerationStrategy() == SqlServerValueGenerationStrategy.IdentityColumn;
            //var isIdentity = c.PropertyInfo
            //    ?.GetCustomAttribute<DatabaseGeneratedAttribute>()
            //    ?.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity;

            if (isIdentity)
            {
                name += " Identity ";
            }

            if (!string.IsNullOrWhiteSpace(c.GetDefaultValueSql()))
            {
                name += " DEFAULT " + c.GetDefaultValueSql();
            }
            return name;
        }

        protected override void DropIndex(SqlIndexEx index)
        {
            Run($"DROP INDEX {index.GetName()} ON { GetTableNameWithSchema(index.DeclaringEntityType) }");
        }

        protected override void CreateIndex(SqlIndexEx index)
        {
            var name = index.GetName();
            var columns = index.Properties;
            var newColumns = columns.Select(x => $"{Escape(x.ColumnName())} ASC").ToJoinString();
            var filter = index.GetFilter() == null ? "" : $" WHERE {index.GetFilter()}";
            Run(@$"CREATE NONCLUSTERED INDEX {name}
                ON {GetTableNameWithSchema(index.DeclaringEntityType)} ({ newColumns })
                {filter}");

        }
    }

    public static class EntityTypeExtensions
    {
        public static string ColumnName(this IProperty property)
        {
            var name = property.DeclaringEntityType.GetTableName();
            var schema = property.DeclaringEntityType.GetSchemaOrDefault();
            var n = property.GetColumnName(StoreObjectIdentifier.Table(name, null));
            return n;
        }
    }

}
