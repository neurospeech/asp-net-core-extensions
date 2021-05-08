using DurableTask.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows.Impl
{
    internal interface IWorkflowActivityInit
    {
        void Set(IServiceProvider sp, MethodInfo method, Type[] argList);
    }

    public class WorkflowActivity<T, TInput, TOutput>: TaskActivity<TInput, TOutput>, IWorkflowActivityInit
    {
        private IServiceProvider sp;
        private MethodInfo method;
        private Type[] argList;
        public TaskContext taskContext;


        protected override async Task<TOutput> ExecuteAsync(TaskContext context, TInput input)
        {
            taskContext = context;
            try
            {
                using (var scope = sp.CreateScope())
                {
                    var proxy = Activator.CreateInstance<T>();
                    var pa = method.GetParameters();
                    var args = new object[pa.Length];
                    int i;
                    int specifiedParameterCount = argList.Length;
                    for (i = 0; i < specifiedParameterCount; i++)
                    {
                        if (specifiedParameterCount == 1)
                        {
                            args[i] = input;
                            continue;
                        }
                        //input is tuple...
                        args[i] = input.GetType().GetProperty($"Item{i + 1}").GetValue(input);
                    }
                    for (; i < pa.Length; i++)
                    {
                        var p = pa[i];
                        if (typeof(IServiceProvider).IsAssignableFrom(p.ParameterType))
                        {
                            args[i] = scope.ServiceProvider;
                            continue;
                        }
                        if (typeof(TaskContext).IsAssignableFrom(p.ParameterType))
                        {
                            args[i] = context;
                            continue;
                        }
                        args[i] = scope.ServiceProvider.GetRequiredService(p.ParameterType);
                    }

                    var task = (Task<TOutput>)method.Invoke(proxy, args);
                    return await task;
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
