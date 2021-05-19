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
        protected DbTransaction Transaction { get; private set; }

        public ModelMigrationBase(DbContext context)
        {
            this.context = context;
            this.columns = new Columns(this);
        }

        internal protected abstract string LoadTableColumns(IEntityType declaringEntityType);

        protected abstract bool IsText(string n);

        protected abstract bool IsDecimal(string n);

        public abstract string Escape(string name);


        protected abstract string GetTableNameWithSchema(IEntityType entity);




        public void Migrate()
        {

            foreach (var entity in context.Model.GetEntityTypes())
            {
                try
                {

                    if (entity.ClrType.GetCustomAttribute<IgnoreMigrationAttribute>() != null)
                        continue;


                    context.Database.OpenConnection();
                    using (var tx = context.Database.GetDbConnection().BeginTransaction(System.Data.IsolationLevel.Serializable))
                    {
                        this.Transaction = tx;

                        MigrateEntity(entity);

                        tx.Commit();
                    }
                }
                finally
                {
                    context.Database.CloseConnection();
                }
            }

        }

        public virtual DbCommand CreateCommand(string command, IEnumerable<KeyValuePair<string, object>> plist = null)
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

        protected virtual void EnsureCreated(IProperty property)
        {
            var fullName = GetTableNameWithSchema(property.DeclaringEntityType) + "." + property.ColumnName();

            var existing = columns[property];
            if (existing != null && existing.IsSame(property))
            {
                return;
            }

            if (existing != null)
            {
                string postFix = $"_{DateTime.UtcNow.Ticks}";
                // rename...
                RenameColumn(property, postFix);

                Console.WriteLine($"Existing Column {fullName} renamed to {fullName}{postFix}.");
            }

            AddColumn(property);

            Console.WriteLine($"Column {fullName} Created Successfully.");
        }

        internal protected abstract string LoadIndexes(IEntityType entity);

        protected virtual void MigrateEntity(IEntityType entity)
        {
            var table = GetTableNameWithSchema(entity);
            EnsureCreated(entity);
            
            Console.WriteLine($"{table} Sync Sucessful.");

            foreach (var property in entity.GetProperties().Where(x => !x.IsKey()))
            {
                EnsureCreated(property);
            }

            // create indexes...
            foreach(var index in entity.GetIndexes())
            {
                EnsureCreated(index);
            }
        }

        protected void EnsureCreated(IIndex index)
        {
            var fullName = GetTableNameWithSchema(index.DeclaringEntityType) + "." + index.GetName();
            var i = new SqlIndexEx(index, this);

            var existing = columns[index];
            if (existing != null && existing.IsSame(i, this))
            {
                return;
            }

            if (existing != null)
            {
                // rename...
                DropIndex(i);
                Console.WriteLine($"Existing Index {fullName} dropped.");
            }

            CreateIndex(i);
            Console.WriteLine($"Index {fullName} Created successfully.");
        }

        protected abstract void DropIndex(SqlIndexEx index);
        protected abstract void CreateIndex(SqlIndexEx index);
        protected abstract void EnsureCreated(IEntityType entity);

        protected abstract void AddColumn(IProperty property);

        protected abstract void RenameColumn(IProperty property, string postFix);


        protected abstract string ToColumn(IProperty c);
    }
}