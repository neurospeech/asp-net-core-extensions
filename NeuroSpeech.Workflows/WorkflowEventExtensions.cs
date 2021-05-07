using DurableTask.Core;
using System;
using System.Threading.Tasks;

namespace NeuroSpeech.Workflows
{
    public static class WorkflowEventExtensions
    {
        public static async Task<(bool Timeout, T Result)> EventWithTimeout<T>(
            this OrchestrationContext context,
            WorkflowEvent<T> wEvent, 
            TimeSpan delay)
        {
            if (wEvent.Event.IsCompleted)
            {
                return (false, await wEvent.Event);
            }
            var timer = context.CreateTimer<bool>(context.CurrentUtcDateTime.Add(delay), true);
            wEvent.Reset();
            await Task.WhenAny(timer, wEvent.Event);

            if (timer.IsCompleted)
                return (true, default);

            return (false, await wEvent.Event);
        }
    }
}
