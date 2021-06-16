using System;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{

    public interface IQueueToken
    {

    }

    public interface IEternityStorage
    {
        Task<IEternityLock> AcquireLockAsync(long sequenceId);
        Task FreeLockAsync(IEternityLock executionLock);
        Task<ActivityStep> GetStatusAsync(ActivityStep key);

        Task<WorkflowStep> GetWorkflowAsync(string id);

        Task<WorkflowStep> InsertAsync(WorkflowStep step);

        /// <summary>
        /// Before queue, check if the workflow exits for the same id which is already completed or failed,
        /// throw and exception
        /// </summary>
        /// <param name="step"></param>
        /// <param name="after"></param>
        /// <returns></returns>
        Task<IQueueToken> QueueWorkflowAsync(string id, DateTimeOffset after);

        Task RemoveQueueAsync(IQueueToken token);

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

        Task<WorkflowStep[]> GetScheduledActivitiesAsync();

        Task UpdateAsync(ActivityStep key);

        Task UpdateAsync(WorkflowStep key);

        /// <summary>
        /// Return not completed/not failed waiting event 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="eventName"></param>
        /// <returns></returns>
        Task<ActivityStep> GetEventAsync(string id, string eventName);
    }

}
