#nullable enable
using DurableTask.Core;
using System;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows.Impl
{
    internal interface IBaseWorkflow
    {
        string? WorkflowID { set; }
    }

    public abstract class BaseWorkflow<TInput, TOutput> 
        : IBaseWorkflow
    {

        public OrchestrationContext? context;

        internal IServiceProvider? serviceProvider;

        internal protected string? WorkflowID { get; internal set; }
        string? IBaseWorkflow.WorkflowID { set => this.WorkflowID = value; }

        public abstract Task<TOutput> RunTask(TInput input);

        internal abstract void OnEvent(string name, string input);

    }
}
