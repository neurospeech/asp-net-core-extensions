using Newtonsoft.Json;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows
{
    public interface IWorkflowEvent
    {
        void SetEvent(string input);
    }

    public class WorkflowEvent<T>: IWorkflowEvent
    {
        public Task<T> Event => taskSoruce.Task;
        private TaskCompletionSource<T> taskSoruce;

        public WorkflowEvent()
        {
            this.taskSoruce = new TaskCompletionSource<T>();
        }

        public void Reset()
        {
            taskSoruce = new TaskCompletionSource<T>();
        }

        public void SetEvent(T result)
        {
            this.taskSoruce.TrySetResult(result);
        }

        void IWorkflowEvent.SetEvent(string input)
        {
            SetEvent(JsonConvert.DeserializeObject<T>(input));
        }
    }
}
