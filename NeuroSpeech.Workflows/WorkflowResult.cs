#nullable enable
using DurableTask.Core;
using Newtonsoft.Json;

namespace NeuroSpeech.Workflows
{
    public class WorkflowResult<T>
    {
        public WorkflowResult(OrchestrationState ctx)
        {
            OrchestrationStatus = ctx.OrchestrationStatus;
            switch (OrchestrationStatus)
            {
                case OrchestrationStatus.Completed:
                    Result = JsonConvert.DeserializeObject<T>(ctx.Output);
                    break;
                case OrchestrationStatus.Failed:
                    Error = ctx.Output;
                    break;

            }
            Status = ctx.Status;
        }

        public T? Result { get; internal set; }

        public string? Status { get; set; }

        public string? Error { get; set; }

        public OrchestrationStatus OrchestrationStatus { get; set; }

        [JsonIgnore]
        public bool Success => OrchestrationStatus == OrchestrationStatus.Completed;

        [JsonIgnore]
        public bool Failed => OrchestrationStatus == OrchestrationStatus.Failed;
    }
}
