using DurableTask.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows.Impl
{
    internal interface IWorkflowActivityInit
    {
        void Set(IServiceProvider sp, MethodInfo method);
    }

    public class WorkflowActivity<T, TInput, TOutput>: TaskActivity<TInput, TOutput>, IWorkflowActivityInit
    {
        private IServiceProvider sp;
        private MethodInfo method;
        public TaskContext taskContext;



        protected override async Task<TOutput> ExecuteAsync(TaskContext context, TInput input)
        {
            taskContext = context;
            using (var scope = sp.CreateScope())
            {
                var proxy = Activator.CreateInstance<T>();
                var pa = method.GetParameters();
                var args = new object[pa.Length];
                for (int i = 0; i < pa.Length; i++)
                {
                    var p = pa[i];
                    if (typeof(IServiceProvider).IsAssignableFrom(p.ParameterType))
                    {
                        args[i] = scope.ServiceProvider;
                        continue;
                    }
                    if (typeof(TInput).IsAssignableFrom(p.ParameterType))
                    {
                        args[i] = input;
                        continue;
                    }
                    if(typeof(TaskContext).IsAssignableFrom(p.ParameterType))
                    {
                        args[i] = context;
                        continue;
                    }
                    args[i] = scope.ServiceProvider.GetRequiredService(p.ParameterType);
                }

                var task = (Task<TOutput>)method.Invoke(proxy, args);
                return await task;
            }
        }

        protected override TOutput Execute(TaskContext context, TInput input)
        {
            throw new NotImplementedException();
        }

        void IWorkflowActivityInit.Set(IServiceProvider sp, MethodInfo method)
        {
            this.sp = sp;
            this.method = method;
        }
    }
}
