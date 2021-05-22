#nullable enable
using DurableTask.Core;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows.Impl
{
    internal interface IWorkflowExecutor<T>
    {
        Task<T> RunAsync(OrchestrationContext context, object input);
    }
}
