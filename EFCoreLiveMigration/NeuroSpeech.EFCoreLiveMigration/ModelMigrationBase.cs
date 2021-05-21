#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using NeuroSpeech.TemplatedQuery;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NeuroSpeech.EFCoreLiveMigration
{

    public abstract class ModelMigrationBase
    {

        protected internal readonly DbContext context;
        private readonly Columns columns;
        protected DbTransaction? Transaction { get; private set; }

        internal MigrationEventList handler = new MigrationEventList();

        public ModelMigrationBase(DbContext context)
        {
            this.context = context;
            this.columns = new Columns(this);
        }

        public ModelMigrationBase AddEvent<T>(MigrationEvents<T> events)
        {
            handler.Add(events);
            return this;
        }

        internal protected abstract string LoadTableColumns(DbTableInfo table);

        protected abstract bool IsText(string n);

        protected abstract bool IsDecimal(string n);

        public abstract string Escape(string name);


        protected abstract string GetTableNameWithSchema(IEntityType entity);



        private List<IEntityType> GetEntityTypes()
        {
            var r = new List<IEntityType>();
            var all = context.Model.GetEntityTypes();
            var pending = new List<IEntityType>();
            var owned = new List<IEntityType>();
            foreach(var entity in all)
            {
                if(entity.BaseType != null)
                {
                    pending.Add(entity);
                    continue;
                }
                if(entity.ClrType.GetCustomAttribute<OwnedAttribute>() != null)
                {
                    owned.Add(entity);
                    continue;
                }
                r.Add(entity);
            }

            r.AddRange(pending);
            r.AddRange(owned);
            return r;
        }

        public void Migrate()
        {

            var entities = GetEntityTypes();

            foreach (var entity in entities)
            {
                try
                {

                    if (entity.ClrType.GetCustomAttribute<IgnoreMigrationAttribute>() != null)
                        continue;


                    context.Database.OpenConnection();
                    using (var tx = context.Database.GetDbConnection().BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        this.Transaction = tx;

                        var table = new DbTableInfo(entity, Escape);
                        MigrateEntity(table);

                        tx.Commit();
                    }
                }
                finally
                {
                    context.Database.CloseConnection();
                }
            }

        }

        public virtual DbCommand CreateCommand(string command, IEnumerable<KeyValuePair<string, object>>? plist = null)
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

        public int Run(TemplateQuery query)
        {
            var cmd = CreateCommand(query.Text, query.Values);
            return cmd.ExecuteNonQuery();
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

        protected virtual void EnsureCreated(DbColumnInfo property, bool forceDefault)
        {
            if (property.Property.DeclaringEntityType != property.Table.EntityType)
                return;

            var existing = columns[property];
            if (existing != null && existing.IsSame(property))
            {
                return;
            }

            if (forceDefault)
            {
                if (!property.IsNullable)
                {
                    if (property.DefaultValue == null)
                        throw new InvalidOperationException($"You must specify the default value for property {property.TableNameAndColumnName} as table contains rows");
                }
            }


            if (existing != null)
            {
                string postFix = $"_{DateTime.UtcNow.Ticks}";
                // rename...
                RenameColumn(property, postFix);
            }

            AddColumn(property);

            if(existing != null)
            {
                property.Table.ColumnsRenamed.Add((existing, property));
            } else
            {
                property.Table.ColumnsAdded.Add(property);
            }
            handler.OnColumnAdded(property, existing);

        }

        internal protected abstract string LoadIndexes(IEntityType entity);

        protected virtual void MigrateEntity(DbTableInfo table)
        {

            this.columns.Clear(table);

            if (!this.columns.Exists(table))
            {
                var keys = table.EntityType.GetProperties().Where(x => x.IsKey())
                    .Select(x => new DbColumnInfo(table, x, Escape))
                    .ToList();
                CreateTable(table, keys);
                handler.OnTableCreated(table);
            }

            var forceDefault = HasAnyRows(table);

            foreach (var property in table.EntityType.GetProperties().Where(x => !x.IsKey()))
            {
                var column = new DbColumnInfo(table, property, Escape);
                EnsureCreated(column, forceDefault);
            }

            // create indexes...
            foreach(var index in table.EntityType.GetIndexes())
            {
               var i = new SqlIndexEx(table,index, this);
                EnsureCreated(i);
            }

            handler.OnTableModified(table, table.ColumnsAdded, table.ColumnsRenamed, table.IndexedUpdated);
        }

        internal protected abstract bool HasAnyRows(DbTableInfo table);

        protected abstract void CreateTable(DbTableInfo entity, List<DbColumnInfo> keys);

        protected void EnsureCreated(SqlIndexEx index)
        {
            var existing = columns[index];
            if (existing != null && existing.IsSame(index, this))
            {
                return;
            }

            if (existing != null)
            {
                // rename...
                DropIndex(index);
                handler.OnIndexDropped(index);
            }

            CreateIndex(index);
            handler.OnIndexCreated(index);
            index.Table.IndexedUpdated.Add((existing != null, index));
        }

        protected abstract void DropIndex(SqlIndexEx index);
        protected abstract void CreateIndex(SqlIndexEx index);
        protected abstract void AddColumn(DbColumnInfo property);

        protected abstract void RenameColumn(DbColumnInfo property, string postFix);


        protected abstract string ToColumn(DbColumnInfo column);
    }
}