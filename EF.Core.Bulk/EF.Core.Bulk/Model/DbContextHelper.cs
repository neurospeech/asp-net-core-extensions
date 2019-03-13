using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Expressions;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.Sql;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace EFCoreBulk
{
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

            var schema = entityType.Relational().Schema;
            var tableName = entityType.Relational().TableName;

            var sql = $"DELETE {queryInfo.Sql.Tables.FirstOrDefault().Alias} FROM ";

            int index = queryInfo.Command.CommandText.IndexOf("FROM ");
            sql += queryInfo.Command.CommandText.Substring(index + 5);

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
                        foreach (var ae in queryInfo.Sql.Projection.OfType<AliasExpression>()) {
                            if (ae.Expression is ColumnExpression ce && ce.Property.IsKey())
                                continue;
                            var p = typeof(T).GetProperty(ae.Alias);
                            p.SetValue(item, p.GetValue(found));
                        }                        
                    }

                    await context.SaveChangesAsync();

                    return list.Count;
                }

                var schema = entityType.Relational().Schema;
                var tableName = entityType.Relational().TableName;

                string setVariables = string.Join(", ",
                    queryInfo.Sql.Projection.OfType<AliasExpression>()
                    .Where(x => !(x.Expression is ColumnExpression ce && ce.Property.IsKey()))
                    .Select(x => $"T1.{x.Alias} = T2.{x.Alias}"));

                string pkeys = "";
                pkeys = string.Join(" AND ", keys.Select(p => $"T1.{p.Name} = T2.{p.Name}"));

                var sql = IsMySqlConnection(context.Database)
                    ? $"UPDATE {tableName} as T1, ({queryInfo.Command.CommandText}) AS T2 SET {setVariables} WHERE {pkeys}"
                    : $"UPDATE T1 SET {setVariables} FROM {tableName} as T1 INNER JOIN ({queryInfo.Command.CommandText}) AS T2 ON {pkeys}";


                sqlGenerated = sql;
                sqlGenerated += "\r\n";
                sqlGenerated += string.Join(",", queryInfo.ParameterValues.ParameterValues.Select(x => x.Key));
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
                    foreach (var p in queryInfo.ParameterValues.ParameterValues)
                    {
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
                var schema = entityType.Relational().Schema;
                var tableName = entityType.Relational().TableName;

                var sql = $"INSERT INTO {tableName} (";

                sql += string.Join(", ",
                    queryInfo.Sql.Projection.OfType<AliasExpression>()
                    .Select(x => x.Alias));

                sql += $")  ({queryInfo.Command.CommandText})";

                return await ExecuteAsync(context, queryInfo, sql);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                throw;
            }
        }

        private static QueryInfo GenerateCommand<T>(DbContext context, IQueryable<T> query, bool forUpdate = false)
        {
            var sp = context as IInfrastructure<IServiceProvider>;

            var ccFactory = sp.GetService<IQueryCompilationContextFactory>();

            QueryCompilationContext cc = ccFactory.Create(true);

            var cacheKeyGenerator = sp.GetService<ICompiledQueryCacheKeyGenerator>();

            IQueryModelGenerator queryModelGenerator = sp.GetService<IQueryModelGenerator>();

            var exp = query.Expression;

            var parameterValues = new Parameters();

            var logger = sp.GetService<IDiagnosticsLogger<DbLoggerCategory.Query>>();

            exp = queryModelGenerator.ExtractParameters(logger, exp, parameterValues);

            var queryModel = queryModelGenerator.ParseQuery(exp);

            var queryModelVisistor = cc.CreateQueryModelVisitor();

            var a = queryModelVisistor.CreateAsyncQueryExecutor<T>(queryModel);


            // query is here
            var rqmv = queryModelVisistor as RelationalQueryModelVisitor;

            var sql = rqmv.Queries.First();

            if (forUpdate) {
                var firstTable = sql.Tables.OfType<TableExpression>().FirstOrDefault();
                var entityType = context.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType == typeof(T));
                var firstExp = sql.Projection.FirstOrDefault() as AliasExpression;

                var existing = new List<Expression>(sql.Projection);

                foreach (var key in entityType.GetKeys().SelectMany(x=>x.Properties)) {
                    if (sql.Projection.OfType<AliasExpression>().Any(e => e.Alias == key.Name))
                        continue;
                    var name = key.Relational().ColumnName;
                    //sql.SetProjectionForMemberInfo(key.PropertyInfo, Expression.Property(firstExp.Expression, key.Name));
                    existing.Add(new AliasExpression(name, new ColumnExpression(name, key, firstTable)));
                }

                // search for literal...
                LiteralExpressionVisitor lv = new LiteralExpressionVisitor(query.Expression);
                foreach (var b in lv.GetLiteralAssignments()) {
                    var p = entityType.GetProperties().FirstOrDefault(x => x.PropertyInfo == b.Member as PropertyInfo);
                    var name = p.Relational().ColumnName;
                    existing.Add(new AliasExpression(name, b.Expression ));
                }
                

                sql.ReplaceProjection(existing);
            }
            

            // System.Diagnostics.Debug.WriteLine(sql);


            ////visitorFactory.Create(queryModelVisitor);


            var factory = sp.GetService<IQuerySqlGeneratorFactory>();


            var sfactory = sp.GetService<ISelectExpressionFactory>();

            //var rqc = cc as RelationalQueryCompilationContext;



            var sf = sfactory.Create(cc as RelationalQueryCompilationContext);
            //sf.Predicate = query.Expression;                

            var def = factory.CreateDefault(sql);

            var cmd = def.GenerateSql(parameterValues.ParameterValues);

            return new QueryInfo
            {
                Command = cmd,
                Sql = sql,
                ParameterValues = parameterValues
            };
        }
    }
}
