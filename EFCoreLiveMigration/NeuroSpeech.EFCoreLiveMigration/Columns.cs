using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Generic;
using System.Linq;

namespace NeuroSpeech.EFCoreLiveMigration
{
    public class Columns
    {
        private ModelMigrationBase modelMigration;

        public Columns(ModelMigrationBase modelMigration)
        {
            this.modelMigration = modelMigration;
            tables = new Dictionary<string, List<Column>>();
            indexes = new Dictionary<string, List<DbIndex>>();
        }

        public Dictionary<string, List<Column>> tables { get; set; }

        public Dictionary<string, List<DbIndex>> indexes { get; set; }


        public Column this[DbColumnInfo property]
        {
            get
            {
                var tableName = property.Table.EscapedNameWithSchema + "." + property.EscapedColumnName;
                if (!tables.TryGetValue(tableName, out var columns))
                {
                    columns = LoadColumns(property.Table);
                }
                return columns.FirstOrDefault(x => x.ColumnName == property.ColumnName);
            }
        }

        public DbIndex this[IIndex index]
        {
            get
            {
                var tableName = index.DeclaringEntityType.GetSchemaOrDefault() + "." + index.DeclaringEntityType.GetTableName();
                if (!indexes.TryGetValue(tableName, out var ind))
                {
                    ind = LoadIndexes(index.DeclaringEntityType);
                }
                return ind.FirstOrDefault(x => x.Name == index.GetName());
            }
        }

        private List<DbIndex> LoadIndexes(IEntityType entity)
        {
            List<DbIndex> list = new List<DbIndex>();
            var indexSql = modelMigration.LoadIndexes(entity);
            var tableName = entity.GetTableName();
            var schameName = entity.GetSchemaOrDefault();
            using (var reader = modelMigration.Read(indexSql, new Dictionary<string, object> {
                { "@TableName", tableName } ,
                { "@SchemaName", schameName }
            }))
            {

                while (reader.Read())
                {
                    var index = new DbIndex();

                    index.Name = reader.GetValue<string>("IndexName");
                    index.Columns = new string[] {
                        reader.GetValue<string>("ColumnName")
                    };
                    index.Filter = reader.GetValue<string>("Filter");
                    list.Add(index);
                }

                list = list.GroupBy(x => x.Name).Select(x => new DbIndex
                {
                    Name = x.Key,
                    Columns = x.SelectMany(c => c.Columns).Select(c => $"[{c}]").ToArray(),
                    Filter = x.First().Filter
                }).ToList();

            }
            return list;
        }

        private List<Column> LoadColumns(DbTableInfo table)
        {
            List<Column> columns = new List<Column>();
            string sqlColumns = modelMigration.LoadTableColumns(table);
            using (var reader = modelMigration.Read(sqlColumns, new Dictionary<string, object> {
                { "@TableName", table.TableName } ,
                { "@SchemaName", table.Schema }
            }))
            {


                while (reader.Read())
                {
                    Column col = new Column();

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

        public void Clear(DbTableInfo entity)
        {
            tables.Remove(entity.EscapedNameWithSchema);
        }

        internal bool Exists(DbTableInfo entity)
        {
            var tableName = entity.EscapedNameWithSchema;
            if (!tables.TryGetValue(tableName, out var columns))
            {
                columns = LoadColumns(entity);
                tables[tableName] = columns;
            }
            return columns.Count > 0;
        }
    }
}
