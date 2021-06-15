using System;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{

    public interface IEternityStorage
    {
        Task<IEternityLock> AcquireLockAsync(long sequenceId);
        Task FreeLockAsync(IEternityLock executionLock);
        Task<ActivityStep> GetStatusAsync(ActivityStep key);

        Task<ActivityStep> GetWorkflowAsync(string id);

        Task QueueWorkflowAsync(ActivityStep step, DateTimeOffset after);

        /// <summary>
        /// ScheduleActivity must set SequenceID which can be used for locking.
        /// 
        /// Set Status to Running always... 
        /// 
        /// Put Workflow Activity on the Queue, not this activity itself
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<ActivityStep> InsertActivityAsync(ActivityStep key);

        Task<ActivityStep[]> GetScheduledActivitiesAsync();

        Task UpdateAsync(ActivityStep key);
    }

}
