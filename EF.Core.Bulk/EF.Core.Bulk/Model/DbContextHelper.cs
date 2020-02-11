using EFCoreBulk.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EFCoreBulk
{

    internal static class ObjectHelper
    {

        internal static FieldInfo GetPrivateFieldInfo(this Type type, string name)
        {
            var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null)
            {
                if (type.BaseType != null)
                    return GetPrivateFieldInfo(type.BaseType, name);
            }
            return f;
        }

        internal static T GetPrivateField<T>(this object target, string name)
            where T :class
        {
            var type = target.GetType();
            var field = type.GetPrivateFieldInfo(name);
            if (field == null)
            {
                throw new MissingFieldException($"Field {name} not found on type {type}");
            }
            var value = field.GetValue(target);
            if (!(value is T tx))
            {
                throw new InvalidCastException($"Unable to cast {value.GetType().FullName} to {typeof(T).FullName}");
            }
            return tx;
        }

    }
    public static class DbContextHelper
    {

        private static bool IsMySqlConnection(DatabaseFacade database)
        {
            return database.ProviderName.Contains("MySql");
        }

        public static Task<int> DeleteAsync<T>(this IQueryable<T> query)
            where T : class
        {
            return DeleteAsync<T>(query.GetDbContext(), query);
        }

        public static Task<int> UpdateAsync<T>(this IQueryable<T> query)
            where T : class
        {
            return UpdateAsync<T>(query.GetDbContext(), query);
        }

        public static Task<int> InsertAsync<T>(this IQueryable<T> query)
            where T : class
        {
            return InsertAsync<T>(query.GetDbContext(), query);
        }

        public static async Task<int> DeleteAsync<T>(this DbContext context, IQueryable<T> query)
            where T:class
        {
            string sql = null;
            try
            {

                if (context.Database.IsInMemory())
                {

                    var list = await query.ToListAsync();
                    context.Set<T>().RemoveRange(list);
                    await context.SaveChangesAsync();
                    return list.Count;
                }

                var queryInfo = GenerateCommand(context, query);

                var entityType = context.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType == typeof(T));

                var schema = entityType.GetSchema();
                var tableName = entityType.GetTableName();

                sql = $"DELETE {queryInfo.Sql.Tables.FirstOrDefault().Alias} FROM ";

                int index = queryInfo.Command.IndexOf("FROM ");
                sql += queryInfo.Command.Substring(index + 5);

                return await ExecuteAsync(context, queryInfo, sql);
            }catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                throw new InvalidOperationException($"Failed to execute: {sql}", ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        public static async Task<int> UpdateAsync<T>(this DbContext context, IQueryable<T> query)
            where T : class
        {
            string sqlGenerated = null;
            try
            {
                var queryInfo = GenerateCommand(context, query, true);

                var entityType = context.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType == typeof(T));
                var keys = entityType.GetKeys().SelectMany(x => x.Properties).ToList();

                if (context.Database.IsInMemory()) {
                    var list = await query.ToListAsync();

                    foreach (var item in context.Set<T>().ToList()) {

                        var found = list.FirstOrDefault(x => keys.All( k => k.PropertyInfo.GetValue(item) == k.PropertyInfo.GetValue(x) ));
                        foreach (var ae in queryInfo.Sql.Projection) {
                            if (keys.Any(k => k.GetColumnName() == ae.Alias))
                            {
                                continue;
                            }
                            var p = typeof(T).GetProperty(ae.Alias);
                            p.SetValue(item, p.GetValue(found));
                        }                        
                    }

                    await context.SaveChangesAsync();

                    return list.Count;
                }

                var schema = entityType.GetSchema();
                var tableName = entityType.GetTableName();

                // LinkedList<string> setVariables = new LinkedList<string>();

                StringBuilder setVariables = new StringBuilder();

                foreach(var p in queryInfo.Sql.Projection)
                {
                    var a = p.Alias;
                    if (string.IsNullOrWhiteSpace(a))
                    {
                        continue;
                    }
                    if (keys.Any(k => k.GetColumnName() == a))
                    {
                        continue;
                    }
                    setVariables.AppendWithSeparator($"T1.{a} = T2.{a}");
                }

                //foreach(var (name,value) in queryInfo.ConstantProjections)
                //{
                //    setVariables.AppendWithSeparator($"T1.{name} = T2.{name}");
                //}

                string pkeys = "";
                pkeys = string.Join(" AND ", keys.Select(p => $"T1.{p.Name} = T2.{p.Name}"));

                var sql = IsMySqlConnection(context.Database)
                    ? $"UPDATE {tableName} as T1, ({queryInfo.Command}) AS T2 SET {setVariables} WHERE {pkeys}"
                    : $"UPDATE T1 SET {setVariables} FROM {tableName} as T1 INNER JOIN ({queryInfo.Command}) AS T2 ON {pkeys}";


                sqlGenerated = sql;
                sqlGenerated += "\r\n";
                //sqlGenerated += queryInfo.Command.CommandText;
                //sqlGenerated += "\r\n";
                sqlGenerated += string.Join(",", queryInfo.ParameterValues.Select(x => x.Key));
                return await ExecuteAsync(context, queryInfo, sql);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                throw new InvalidOperationException($"Failed to execute: {sqlGenerated}", ex);
            }
        }

        private static async Task<int> ExecuteAsync(DbContext context, QueryInfo queryInfo, string sql)
        {
            var db = context.Database.GetDbConnection();
            try
            {
                await context.Database.OpenConnectionAsync();
                bool isMySQL = IsMySqlConnection(context.Database);
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandText = sql;
                    var t = context.Database.CurrentTransaction;
                    if (t != null)
                    {
                        cmd.Transaction = t.GetDbTransaction();
                    }

                    foreach (var p in queryInfo.ParameterValues)
                    {
                        // since array and list are expanded inline, ignore them
                        if (!(p.Value is string) && p.Value is System.Collections.IEnumerable)
                        {
                            continue;
                        }
                        if (! (p.Value is string || (p.Value.GetType()?.IsValueType ?? false)))
                        {
                            continue;
                        }
                        var cp = cmd.CreateParameter();
                        if (isMySQL)
                        {
                            cp.ParameterName = $":p{p.Key}";
                        }
                        else
                        {
                            cp.ParameterName = p.Key;
                        }
                        cp.Value = p.Value;
                        cmd.Parameters.Add(cp);
                    }

                    return await cmd.ExecuteNonQueryAsync();
                }
            }
            finally {
                context.Database.CloseConnection();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static async Task<int> InsertAsync<T>(this DbContext context, IQueryable<T> query)
            where T: class
        {

            try
            {
                if (context.Database.IsInMemory())
                {
                    var list = await query.ToListAsync();
                    context.Set<T>().AddRange(list);
                    await context.SaveChangesAsync();
                    return list.Count;
                }
                var queryInfo = GenerateCommand(context, query);
                var entityType = context.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType == typeof(T));
                var schema = entityType.GetSchema();
                var tableName = entityType.GetTableName();

                var sql = $"INSERT INTO {tableName} (";

                sql += string.Join(", ",
                    queryInfo.Sql.Projection.OfType<ProjectionExpression>()
                    .Select(x => x.Alias));

                sql += $")  ({queryInfo.Command})";

                return await ExecuteAsync(context, queryInfo, sql);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                throw;
            }
        }

        public static (string, IReadOnlyDictionary<string, object>, SelectExpression, SqlExpressionFactory) ToSqlWithParams<TEntity>(
            this IQueryable<TEntity> query,
            SelectExpression sql = null)
        {
            var enumerator = query.Provider
                .Execute<IEnumerable<TEntity>>(query.Expression)
                .GetEnumerator();
            var enumeratorType = enumerator.GetType();

            var commandCache = enumerator.GetPrivateField<RelationalCommandCache>("_relationalCommandCache");

            var selectExpression = commandCache.GetPrivateField<SelectExpression>("_selectExpression");
            var factory = commandCache.GetPrivateField<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");
            var queryContext = enumerator.GetPrivateField<RelationalQueryContext>("_relationalQueryContext");

            var optimizer = commandCache.GetPrivateField<object>("_parameterValueBasedSelectExpressionOptimizer");

            var typeMappingSource = optimizer.GetPrivateField<SqlExpressionFactory>("_sqlExpressionFactory");

            var sqlGenerator = factory.Create();

            // selectExpression = FixCastErrorExpressionVisitor.Fix(selectExpression);

            var dependencies = factory.GetPrivateField<QuerySqlGeneratorDependencies>("_dependencies");

            if (sql != null)
            {
                selectExpression = sql;
            }
            var translator = new FixCastErrorExpressionVisitor(dependencies, queryContext, typeMappingSource);

            selectExpression = translator.FixError(selectExpression);

            var command = sqlGenerator.GetCommand(selectExpression);
            
            var parametersDict = queryContext.ParameterValues;
            return (command.CommandText, parametersDict, selectExpression, typeMappingSource);
        }

        private static QueryInfo GenerateCommand<T>(DbContext context, IQueryable<T> query, bool forUpdate = false)
        {

            var (command, paramList, sql, factory) = query.ToSqlWithParams();


            if (forUpdate) {

                var firstTable = sql.Tables.OfType<TableExpression>().FirstOrDefault();
                var entityType = context.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType == typeof(T));
                var firstExp = sql.Tables.FirstOrDefault();

                var existing = new List<ProjectionExpression>(sql.Projection);

                var schema = entityType.GetSchema();
                var tableName = entityType.GetTableName();

                // add primary key..
                var ke = new EntityProjectionExpression(entityType, firstTable, false);
                // var keyColumns = entityType.GetProperties()
                // .Where(x => x.IsPrimaryKey())
                // .Select(x => ke.BindProperty(x));
                // sql.AddToProjection(ke);

                var keyColumns = entityType.GetProperties().Where(x => x.IsPrimaryKey());

                foreach(var kc in keyColumns)
                {

                    //var columnName = kc.GetColumnName();

                    //if (sql.Projection.Any((x) => x.Alias == columnName))
                    //    continue;

                    //var fx = factory.Fragment($"[{firstTable.Alias}].[{columnName}]");

                    sql.AddToProjection(ke.BindProperty(kc));
                }

                // search for literal...
                LiteralExpressionVisitor lv = new LiteralExpressionVisitor();
                foreach(var (me, c) in lv.GetLiteralAssignments(query.Expression))
                {
                    var p = entityType.GetProperties().FirstOrDefault(x => x.Name == me.Name);
                    var columnName = p.GetColumnName();
                    if (sql.Projection.Any((x) => x.Alias == columnName))
                        continue;
                    var ce = factory.Constant(c, factory.GetTypeMappingForValue(c ));
                    var pe = new ProjectionExpression(ce, columnName);
                    (sql.Projection as IList<ProjectionExpression>).Add(pe);
                }
                //foreach (var b in lv.GetLiteralAssignments())
                //{
                //    var p = entityType.GetProperties().FirstOrDefault(x => x.PropertyInfo == b.Member as PropertyInfo);
                //    var name = p.GetColumnName();

                //    // sql.AddToProjection(new SqlConstantExpression())
                //    // existing.Add(new AliasExpression(name, b.Expression ));
                //}

                (command, paramList, sql, factory) = query.ToSqlWithParams(sql);


                //sql.ReplaceProjection(existing);
            }


            // System.Diagnostics.Debug.WriteLine(sql);


            ////visitorFactory.Create(queryModelVisitor);


            return new QueryInfo
            {
                Command = command,
                Sql = sql,
                ParameterValues = paramList
            };
        }
    }
}
