using DurableTask.Core;
using System;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows.Impl
{
    internal interface IWorkflow<T>
    {
        Task<T> RunAsync(OrchestrationContext context, object input);
    }



    public class WorkflowExecutor<T,TInput, TOutput> : TaskOrchestration<TOutput, TInput>, IWorkflow<TOutput>
        where T: Workflow<TInput, TOutput>
    {
        private readonly IServiceProvider sp;
        private readonly T workflow;

        public WorkflowExecutor(IServiceProvider sp)
        {
            this.sp = sp;
            this.workflow = sp.Build<T>();
        }
        public override Task<TOutput> RunTask(OrchestrationContext context, TInput input)
        {
            workflow.context = context;
            workflow.serviceProvider = sp;
            return workflow.RunTask(input);
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            workflow.context = context;
            workflow.serviceProvider = sp;
            workflow.OnEvent(name, input);
        }

        Task<TOutput> IWorkflow<TOutput>.RunAsync(OrchestrationContext context, object input)
        {
            return RunTask(context, (TInput)input);
        }
    }
}
