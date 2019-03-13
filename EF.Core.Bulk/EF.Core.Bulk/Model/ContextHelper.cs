using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Linq;
using System.Reflection;

namespace EFCoreBulk
{
    public static class ContextHelper
    {
        public static DbContext GetDbContext<T>(this IQueryable<T> source)
        {
            var compilerField = typeof(EntityQueryProvider).GetField("_queryCompiler", BindingFlags.NonPublic | BindingFlags.Instance);
            var compiler = (QueryCompiler)compilerField.GetValue(source.Provider);

            var queryContextFactoryField = compiler.GetType().GetField("_queryContextFactory", BindingFlags.NonPublic | BindingFlags.Instance);
            var obj = queryContextFactoryField.GetValue(compiler);
            var queryContextFactory = (QueryContextFactory)obj;



            object stateManagerDynamic;

            var dependenciesProperty = queryContextFactory.GetType().GetProperty("Dependencies", BindingFlags.NonPublic | BindingFlags.Instance);
            if (dependenciesProperty != null)
            {
                // EFCore 2.x
                var dependencies = dependenciesProperty.GetValue(queryContextFactory);

                var stateManagerField = typeof(DbContext).GetTypeFromAssembly_Core("Microsoft.EntityFrameworkCore.Query.QueryContextDependencies").GetProperty("StateManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                stateManagerDynamic = stateManagerField.GetValue(dependencies);
            }
            else
            {
                // EFCore 1.x
                var stateManagerField = typeof(QueryContextFactory).GetProperty("StateManager", BindingFlags.NonPublic | BindingFlags.Instance);
                stateManagerDynamic = stateManagerField.GetValue(queryContextFactory);
            }

            IStateManager stateManager = stateManagerDynamic as IStateManager;

            if (stateManager == null)
            {
                Microsoft.EntityFrameworkCore.Internal.LazyRef<IStateManager> lazyStateManager = stateManagerDynamic as Microsoft.EntityFrameworkCore.Internal.LazyRef<IStateManager>;
                if (lazyStateManager != null)
                {
                    stateManager = lazyStateManager.Value;
                }
            }

            if (stateManager == null)
            {
                stateManager = ((dynamic)stateManagerDynamic).Value;
            }

            return stateManager.Context;
        }


        internal static Type GetTypeFromAssembly_Core(this Type fromType, string name)
        {
            return fromType.Assembly.GetType(name);
        }
    }
}
