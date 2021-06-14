using System;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{
    public interface IEternityStorage
    {
        Task<long> AcquireLockAsync(long sequenceId);
        Task FreeLockAsync(long executionLock);
        Task<ActivityStep> GetStatusAsync(ActivityStep key);

        /// <summary>
        /// ScheduleActivity must set SequenceID which can be used for locking
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<ActivityStep> ScheduleActivityAsync(ActivityStep key);
        Task<ActivityStep[]> GetScheduledActivitiesAsync();
        Task UpdateAsync(ActivityStep key);
    }

}
