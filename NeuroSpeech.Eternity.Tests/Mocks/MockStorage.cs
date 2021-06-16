using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity.Tests.Mocks
{

    public class MockLock: IEternityLock {
        public readonly string id;

        public MockLock(string id)
        {
            this.id = id;
        }
    
    }

    public class MockStorage : IEternityStorage
    {
        private readonly IEternityClock clock;
        private MockDatabase db = new MockDatabase();
        private ConcurrentDictionary<string, IEternityLock> locks = new ConcurrentDictionary<string, IEternityLock>();

        public MockStorage(IEternityClock clock)
        {
            this.clock = clock;
        }

        public async Task<IEternityLock> AcquireLockAsync(long sequenceId)
        {
            var key = sequenceId.ToString();
            var newLock = new MockLock(key);
            while(true)
            {
                if(locks.TryAdd(key, newLock))
                {
                    return newLock;
                }
                await Task.Delay(100);
                continue;
            }
        }

        public Task FreeLockAsync(IEternityLock executionLock)
        {
            locks.TryRemove((executionLock as MockLock).id, out var n);
            return Task.CompletedTask;
        }

        public Task<ActivityStep> GetEventAsync(string id, string eventName)
        {
            return Task.FromResult(db.GetEventAsync(id, eventName));
        }

        public Task<ActivityStep[]> GetScheduledActivitiesAsync()
        {
            return Task.FromResult(db.GetReadyAsync(clock.UtcNow));
        }

        public Task<ActivityStep> GetStatusAsync(ActivityStep key)
        {
            return db.SearchAsync(key.ID, key.ActivityType, key.KeyHash, key.Key);
        }

        public Task<ActivityStep> GetWorkflowAsync(string id)
        {
            return db.SearchAsync(id, ActivityType.Workflow);
        }

        public Task<ActivityStep> InsertActivityAsync(ActivityStep key)
        {
            return db.InsertAsync(key);
        }

        public Task QueueWorkflowAsync(ActivityStep step, DateTimeOffset after)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ActivityStep key)
        {
            return db.UpdateAsync(key);
        }
    }
}
