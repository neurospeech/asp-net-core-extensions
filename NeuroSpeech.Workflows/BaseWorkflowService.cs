using DurableTask.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows
{
    public class BaseWorkflowService
    {
        internal protected TaskHubClient client;

        public async Task RaiseEvent(string id, string name, object data = null)
        {
            var state = await client.GetOrchestrationStateAsync(id);
            await client.RaiseEventAsync(state.OrchestrationInstance, name, data ?? "");
        }

    }
}
