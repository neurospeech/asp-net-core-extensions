#nullable enable
using DurableTask.Core;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows.Impl
{



    public class WorkflowExecutor<T,TInput, TOutput> : TaskOrchestration<TOutput, TInput>, IWorkflowExecutor<TOutput>
        where T: BaseWorkflow<TInput, TOutput>
    {
        private readonly IServiceProvider sp;
        private readonly T workflow;

        public WorkflowExecutor(IServiceProvider sp)
        {
            this.sp = sp;
            this.workflow = sp.Build<T>();
        }
        public override async Task<TOutput> RunTask(OrchestrationContext context, TInput input)
        {

            TOutput result = default!;

            async Task RunInternalAsync(TInput input) {
                result = await workflow.RunTask(input);
            }
            var ct = context.GetCancellationTokenSource().Token;
            workflow.context = context;
            workflow.serviceProvider = sp;
            workflow.WorkflowID = context.OrchestrationInstance.InstanceId;

            var completed = new CancellationTokenSource();

            await Task.WhenAny( Task.Delay(TimeSpan.FromMilliseconds(-1), completed.Token), RunInternalAsync(input) );

            context.RemoveCancellationTokenSource();
            completed.Cancel();

            if(ct.IsCancellationRequested)
            {
                throw new TaskCanceledException($"Task was cancelled by cancel event");
            }
            return result;
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if(name == "__CANCEL")
            {
                context.GetCancellationTokenSource().Cancel();
                return;
            }

            workflow.context = context;
            workflow.serviceProvider = sp;
            workflow.WorkflowID = context.OrchestrationInstance.InstanceId;
            workflow.OnEvent(name, input);
        }

        Task<TOutput> IWorkflowExecutor<TOutput>.RunAsync(OrchestrationContext context, object input)
        {
            return RunTask(context, (TInput)input);
        }
    }
}
