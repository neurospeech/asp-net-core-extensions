using DurableTask.Core;
using System;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows.Impl
{
    internal interface IWorkflowExecutor<T>
    {
        Task<T> RunAsync(OrchestrationContext context, object input);
    }

    public abstract class BaseWorkflow<TInput, TOutput> {

        public OrchestrationContext? context;

        internal IServiceProvider? serviceProvider;

        internal abstract void SetupEvents();

        public abstract Task<TOutput> RunTask(TInput input);

        internal abstract void OnEvent(string name, string input);
    }


    public class WorkflowExecutor<T,TInput, TOutput> : TaskOrchestration<TOutput, TInput>, IWorkflowExecutor<TOutput>
        where T: BaseWorkflow<TInput, TOutput>
    {
        private readonly IServiceProvider sp;
        private readonly T workflow;

        public WorkflowExecutor(IServiceProvider sp)
        {
            this.sp = sp;
            this.workflow = sp.Build<T>();

            // setup events..
            this.workflow.SetupEvents();
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

        Task<TOutput> IWorkflowExecutor<TOutput>.RunAsync(OrchestrationContext context, object input)
        {
            return RunTask(context, (TInput)input);
        }
    }
}
