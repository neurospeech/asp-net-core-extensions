using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace EFCoreBulk
{

    internal static class ObjectHelper
    {

        internal static object GetPrivateField(this object target, string name)
        {
            var type = target.GetType();
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(target);
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

            if (context.Database.IsInMemory()) {

                var list = await query.ToListAsync();
                context.Set<T>().RemoveRange(list);
                await context.SaveChangesAsync();
                return list.Count;
            }

            var queryInfo = GenerateCommand(context, query);

            var entityType = context.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType == typeof(T));

            var schema = entityType.GetSchema();
            var tableName = entityType.GetTableName();

            var sql = $"DELETE {queryInfo.Sql.Tables.FirstOrDefault().Alias} FROM ";

            int index = queryInfo.Command.IndexOf("FROM ");
            sql += queryInfo.Command.Substring(index + 5);

            return await ExecuteAsync(context, queryInfo, sql);
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
                var keys = entityType.GetKeys().SelectMany(x => x.Properties);

                if (context.Database.IsInMemory()) {
                    var list = await query.ToListAsync();

                    foreach (var item in context.Set<T>().ToList()) {

                        var found = list.FirstOrDefault(x => keys.All( k => k.PropertyInfo.GetValue(item) == k.PropertyInfo.GetValue(x) ));
                        foreach (var ae in queryInfo.Sql.Projection.OfType<ProjectionExpression>()) {
                            //if (ae.Expression is ColumnExpression ce && ce..IsKey())
                            //    continue;
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

                string setVariables = string.Join(", ",
                    queryInfo.Sql.Projection
                    .Where(x => !(x.Expression is ColumnExpression ce))
                    .Select(x => $"T1.{x.Alias} = T2.{x.Alias}"));

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

        public static (string, IReadOnlyDictionary<string, object>, SelectExpression) ToSqlWithParams<TEntity>(this IQueryable<TEntity> query)
        {
            var provider = query.Provider;

            var queryCompiler = provider.GetPrivateField("_queryCompiler");
            var db = queryCompiler.GetPrivateField("_database") as IDatabase;
            var dp = db.GetType().GetProperty("Dependencies", BindingFlags.Instance | BindingFlags.NonPublic);

            var dps = dp.GetValue(db);

            // providerType.GetField("_")

            var enumerator = query.Provider
                .Execute<IEnumerable<TEntity>>(query.Expression)
                .GetEnumerator();
            const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            var enumeratorType = enumerator.GetType();
            var selectFieldInfo = enumeratorType.GetField("_selectExpression", bindingFlags) ?? throw new InvalidOperationException($"cannot find field _selectExpression on type {enumeratorType.Name}");
            var sqlGeneratorFieldInfo = enumeratorType.GetField("_querySqlGeneratorFactory", bindingFlags) ?? throw new InvalidOperationException($"cannot find field _querySqlGeneratorFactory on type {enumeratorType.Name}");
            var queryContextFieldInfo = enumeratorType.GetField("_relationalQueryContext", bindingFlags) ?? throw new InvalidOperationException($"cannot find field _relationalQueryContext on type {enumeratorType.Name}");

            var selectExpression = selectFieldInfo.GetValue(enumerator) as SelectExpression ?? throw new InvalidOperationException($"could not get SelectExpression");
            var factory = sqlGeneratorFieldInfo.GetValue(enumerator) as IQuerySqlGeneratorFactory ?? throw new InvalidOperationException($"could not get SqlServerQuerySqlGeneratorFactory");
            var queryContext = queryContextFieldInfo.GetValue(enumerator) as RelationalQueryContext ?? throw new InvalidOperationException($"could not get RelationalQueryContext");

            var sqlGenerator = factory.Create();
            var command = sqlGenerator.GetCommand(selectExpression);
            
            var parametersDict = queryContext.ParameterValues;
            var sql = command.CommandText;
            return (sql, parametersDict, selectExpression);
        }

        private static QueryInfo GenerateCommand<T>(DbContext context, IQueryable<T> query, bool forUpdate = false)
        {

            var (command, paramList, sql) = query.ToSqlWithParams();

            if (forUpdate) {
                var firstTable = sql.Tables.OfType<TableExpression>().FirstOrDefault();
                var entityType = context.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType == typeof(T));
                var firstExp = sql.Tables.FirstOrDefault();

                var existing = new List<ProjectionExpression>(sql.Projection);

                var schema = entityType.GetSchema();
                var tableName = entityType.GetTableName();

                // add primary key..
                var ke = new EntityProjectionExpression(entityType, firstTable, false);
                var keyColumns = entityType.GetProperties()
                    .Where(x => x.IsPrimaryKey())
                    .Select(x => ke.BindProperty(x));
                foreach(var kc in keyColumns)
                {
                    sql.AddToProjection(kc);
                }

                // search for literal...
                LiteralExpressionVisitor lv = new LiteralExpressionVisitor();
                lv.GetLiteralAssignments(sql, ke, entityType, query.Expression);
                //foreach (var b in lv.GetLiteralAssignments())
                //{
                //    var p = entityType.GetProperties().FirstOrDefault(x => x.PropertyInfo == b.Member as PropertyInfo);
                //    var name = p.GetColumnName();
                    
                //    // sql.AddToProjection(new SqlConstantExpression())
                //    // existing.Add(new AliasExpression(name, b.Expression ));
                //}

                (command, paramList, sql) = query.ToSqlWithParams();

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
