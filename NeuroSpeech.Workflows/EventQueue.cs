#nullable enable
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows
{
    public interface IWorkflowEvent
    {
        void SetEvent(string input);
    }

    public class WorkflowEvent
    {

        internal WorkflowEvent(string name)
        {
            this.Name = name;
        }

        internal readonly string Name;

        /// <summary>
        /// Raises the event for given instance id
        /// </summary>
        /// <param name="service"></param>
        /// <param name="id"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task RaiseAsync(BaseWorkflowService service, string id, string data = "")
        {
            var ctx = await service.client.GetOrchestrationStateAsync(id);
            await service.client.RaiseEventAsync(ctx.OrchestrationInstance, Name, data);
        }
    }

    public class EventQueue
    {
        private Queue<string>? pending;
        private TaskCompletionSource<string>? taskSoruce;
        private CancellationTokenSource? cancelSource;
        
        public EventQueue()
        {
        }

        public void Reset()
        {
            cancelSource?.Cancel();
            taskSoruce = null;
            cancelSource = null;
        }

        public void SetEvent(string result)
        {
            if(taskSoruce == null)
            {
                pending ??= new Queue<string>();
                pending.Enqueue(result);
                return;
            }
            cancelSource?.Cancel();
            this.taskSoruce.TrySetResult(result);
        }

        public (Task<string> waiter, CancellationToken token) Request()
        {
            if(pending?.Count > 0)
            {
                return (Task.FromResult(pending.Dequeue()), default);
            }
            taskSoruce = new TaskCompletionSource<string>();
            cancelSource = new CancellationTokenSource();
            return (taskSoruce.Task, cancelSource.Token);
        }
    }
}
