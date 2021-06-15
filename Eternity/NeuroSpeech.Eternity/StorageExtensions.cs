using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{
    internal static class StorageExtensions
    {
        public static async Task<ActivityStep> ScheduleActivityAsync(this IEternityStorage storage, ActivityStep step)
        {
            var eta = step.ETA;
            if (step.SequenceID == 0)
            {
                step = await storage.InsertActivityAsync(step);
            }
            var original = step;
            if (step.ActivityType != ActivityType.Workflow)
            {
                step = await storage.GetWorkflowAsync(step.ID);
            }
            await storage.QueueWorkflowAsync(step, eta);
            return original;
        }
    }
}
