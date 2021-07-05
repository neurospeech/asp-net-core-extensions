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


        internal protected override string LoadTableColumns(DbTableInfo declaringEntityType)
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

        protected override void AddColumn(DbColumnInfo property)
        {
            Run($"ALTER TABLE {property.Table.EscapedNameWithSchema} ADD " 
                + ToColumn(property));
        }

        protected override void RenameColumn(DbColumnInfo property, string postFix)
        {
            Run($"EXEC sp_rename '{property.Table.Schema}.{property.Table.TableName}.{property.ColumnName}', '{property.ColumnName}{postFix}'");
        }

        protected override void CreateTable(
            DbTableInfo entity, 
            List<DbColumnInfo> keys)
        {
            var createTable = TemplateQuery.Literal(@$"
                    CREATE TABLE {entity.EscapedNameWithSchema} ({ string.Join(",", keys.Select(ToColumn)) }, 
                    PRIMARY KEY( { string.Join(", ", keys.Select(x => x.EscapedColumnName )) } ))");

            Run(createTable);
        }


        private static string[] textTypes = new[] { "nvarchar", "varchar" };

        protected override bool IsText(string n) => textTypes.Any(a => a.Equals(n, StringComparison.OrdinalIgnoreCase));

        protected override bool IsDecimal(string n) => n.Equals("decimal", StringComparison.OrdinalIgnoreCase);

        protected override string GetTableNameWithSchema(IEntityType entity)
        {
            return $"{Escape(entity.GetSchemaOrDefault())}.{Escape(entity.GetTableName())}";
        }

        protected internal override bool HasAnyRows(DbTableInfo table)
        {
            var sql = $"SELECT TOP (1) 1 FROM {table.EscapedNameWithSchema}";
            var cmd = CreateCommand(sql);
            var i = cmd.ExecuteScalar();
            if (i == null)
                return false;
            return true;
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
                var np = c.Precision ?? 18;
                var nps = c.DecimalScale ?? 2;

                name += $"({ np },{ nps })";
            }
            if (!c.IsKey)
            {
                // lets allow nullable to every field...
                if (c.IsNullable)
                {
                    name += " NULL ";
                }
                else
                {
                    name += " NOT NULL ";
                }
            }

            if (c.IsIdentity)
            {
                name += " Identity ";
            }

            if (!string.IsNullOrWhiteSpace(c.DefaultValue))
            {
                name += " DEFAULT " + c.DefaultValue;
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
            var indexType = index.Unique ? " UNIQUE " : " NONCLUSTERED ";
            Run(@$"CREATE {indexType} INDEX {name}
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
#if NET_STANDARD_2_1
            var n = property.GetColumnName(StoreObjectIdentifier.Table(name, null));
#else
            var n = property.GetColumnName();
#endif
            return n;
        }
    }

}
