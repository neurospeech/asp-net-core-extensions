using DurableTask.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows.Impl
{
    internal interface IWorkflowActivityInit
    {
        void Set(IServiceProvider sp, MethodInfo method, Type[] argList);
    }

    public delegate Task<TOutput> ActivityFunction<TOutput>(IServiceProvider serviceProvider, TaskContext context, object input);

    public class WorkflowActivity<T, TInput, TOutput>: TaskActivity<TInput, TOutput>, IWorkflowActivityInit
    {
        private IServiceProvider sp;
        private MethodInfo method;
        private Type[] argList;

        private static Dictionary<string, ActivityFunction<TOutput>> functions 
            = new Dictionary<string, ActivityFunction<TOutput>>();

        private static ActivityFunction<TOutput> Function(MethodInfo method , Type[] argList)
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

            var getRequiredService = typeof(ServiceProviderServiceExtensions)
                    .GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService));

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
                // args[i] = scope.ServiceProvider.GetRequiredService(p.ParameterType);
                arguments.Add(Expression.Call(sp, getRequiredService,  Expression.Constant(p.ParameterType)));
            }

            var call = Expression.Call(Expression.New(typeof(T)), method, arguments);

            var fxc = Expression
                .Lambda<ActivityFunction<TOutput>>(call, sp, tc, input)
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

                    return await fxc(scope.ServiceProvider, context, input);

                    //var proxy = Activator.CreateInstance<T>();
                    //var pa = method.GetParameters();
                    //var args = new object[pa.Length];
                    //int i;
                    //int specifiedParameterCount = argList.Length;
                    //for (i = 0; i < specifiedParameterCount; i++)
                    //{
                    //    if (specifiedParameterCount == 1)
                    //    {
                    //        args[i] = input;
                    //        continue;
                    //    }
                    //    //input is tuple...
                    //    args[i] = input.GetType().GetProperty($"Item{i + 1}").GetValue(input);
                    //}
                    //for (; i < pa.Length; i++)
                    //{
                    //    var p = pa[i];
                    //    if (typeof(IServiceProvider).IsAssignableFrom(p.ParameterType))
                    //    {
                    //        args[i] = scope.ServiceProvider;
                    //        continue;
                    //    }
                    //    if (typeof(TaskContext).IsAssignableFrom(p.ParameterType))
                    //    {
                    //        args[i] = context;
                    //        continue;
                    //    }
                    //    args[i] = scope.ServiceProvider.GetRequiredService(p.ParameterType);
                    //}

                    //var task = (Task<TOutput>)method.Invoke(proxy, args);
                    // return await task;
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
