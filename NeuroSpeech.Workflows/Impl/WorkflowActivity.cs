using DurableTask.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows.Impl
{
    internal interface IWorkflowActivityInit
    {
        void Set(IServiceProvider sp, MethodInfo method, Type[] argList);
    }

    public delegate Task<TOutput> ActivityFunction<TInput, TOutput>(
        IServiceProvider serviceProvider, 
        TaskContext context, 
        TInput input,
        string id);

    public class WorkflowActivity<T, TInput, TOutput>: TaskActivity<TInput, TOutput>, IWorkflowActivityInit
        where T: new()
    {
        private IServiceProvider sp;
        private MethodInfo method;
        private Type[] argList;

        private static Dictionary<string, ActivityFunction<TInput, TOutput>> functions 
            = new Dictionary<string, ActivityFunction<TInput, TOutput>>();

        private static MethodInfo getRequiredService = typeof(ServiceProviderServiceExtensions)
                    .GetMethods()
                    .First(x => x.Name == nameof(ServiceProviderServiceExtensions.GetRequiredService)
                        && x.IsGenericMethod
                        && x.GetParameters().Length == 1);

        private static MethodInfo getCancellationToken
            = typeof(CancelTokenExtensions).GetMethod(nameof(CancelTokenExtensions.GetCancellationToken),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(TaskContext) }, null);


        private static ActivityFunction<TInput, TOutput> Function(MethodInfo method , Type[] argList)
        {
            var key = method.DeclaringType.FullName + "_" + method.Name;
            if (functions.TryGetValue(key, out var fx))
                return fx;
            var sp = Expression.Parameter(typeof(IServiceProvider));
            var tc = Expression.Parameter(typeof(TaskContext));
            var input = Expression.Parameter(typeof(TInput));

            var specifiedParameterCount = argList.Length;
            Type tupleType = null;
            if (specifiedParameterCount > 1)
            {
                tupleType = argList.ToTuple();
            }
            var pa = method.GetParameters();


            List <Expression> arguments = new List<Expression>();
            var i = 0;
            for (; i < specifiedParameterCount; i++)
            {
                if(specifiedParameterCount == 1)
                {
                    arguments.Add(input);
                    continue;
                }
                arguments.Add(Expression.Property(input, tupleType.GetProperty("Item" + (i+1)) ));
            }
            for (; i < pa.Length; i++)
            {
                var p = pa[i];
                if (typeof(IServiceProvider).IsAssignableFrom(p.ParameterType))
                {
                    arguments.Add(sp);
                    continue;
                }
                if (typeof(TaskContext).IsAssignableFrom(p.ParameterType))
                {
                    arguments.Add(tc);
                    continue;
                }
                if(typeof(System.Threading.CancellationToken) == p.ParameterType)
                {
                    arguments.Add(Expression.Call(null, getCancellationToken, tc));
                    continue;
                }
                // args[i] = scope.ServiceProvider.GetRequiredService(p.ParameterType);
                arguments.Add(Expression.Call(null, getRequiredService.MakeGenericMethod(p.ParameterType), sp));
            }

            var wa = Expression.Parameter(typeof(T));

            var call = 
                Expression.Call(wa, method, arguments);

            var wid = Expression.Parameter(typeof(string));

            var body = Expression.Block(
                new ParameterExpression[] { wa },
                Expression.Assign(Expression.Property(wa, "WorkflowID"),wid),
                call
                );

            var fxc = Expression
                .Lambda<ActivityFunction<TInput, TOutput>>(
                body
                , sp, tc, input, wid)
                .Compile();
            functions[key] = fxc;
            return fxc;
        }

        protected override async Task<TOutput> ExecuteAsync(TaskContext context, TInput input)
        {
            try
            {
                using (var scope = sp.CreateScope())
                {
                    var fxc = Function(method, argList);
                    var w = new T();
                    ((IBaseWorkflow)w).WorkflowID = context.OrchestrationInstance.InstanceId;
                    return await fxc(scope.ServiceProvider, context, input, context.OrchestrationInstance.InstanceId);
                }
            } catch (Exception ex)
            {
                // DTX swallows exception details...
                throw new InvalidOperationException(ex.ToString());
            }
        }

        protected override TOutput Execute(TaskContext context, TInput input)
        {
            throw new NotImplementedException();
        }

        void IWorkflowActivityInit.Set(IServiceProvider sp, MethodInfo method, Type[] argList)
        {
            this.sp = sp;
            this.method = method;
            this.argList = argList;
        }
    }
}
