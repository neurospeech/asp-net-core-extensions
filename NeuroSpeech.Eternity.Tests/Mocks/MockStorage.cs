using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity.Tests.Mocks
{
    public class MockStorage : IEternityStorage
    {
        private Dictionary<string, ActivityStep> storage = new Dictionary<string, ActivityStep>();

        private Queue<ActivityStep> queue = new Queue<ActivityStep>();

        public Task<IEternityLock> AcquireLockAsync(long sequenceId)
        {
            throw new NotImplementedException();
        }

        public Task FreeLockAsync(IEternityLock executionLock)
        {
            throw new NotImplementedException();
        }

        public Task<ActivityStep[]> GetScheduledActivitiesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<ActivityStep> GetStatusAsync(ActivityStep key)
        {
            throw new NotImplementedException();
        }

        public Task<ActivityStep> GetWorkflowAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<ActivityStep> InsertActivityAsync(ActivityStep key)
        {
            throw new NotImplementedException();
        }

        public Task QueueWorkflowAsync(ActivityStep step, DateTimeOffset after)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(ActivityStep key)
        {
            throw new NotImplementedException();
        }
    }
}
