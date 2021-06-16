using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private ConcurrentDictionary<string, IEternityLock> locks = new ConcurrentDictionary<string, IEternityLock>();

        private List<ActivityStep> list = new List<ActivityStep>();
        private List<WorkflowStep> workflows = new List<WorkflowStep>();
        private List<QueueToken> queue = new List<QueueToken>();


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
            var e = list.FirstOrDefault(x => x.ID == id
                && x.ActivityType == ActivityType.Event
                && x.Status != ActivityStatus.Completed
                && x.Status != ActivityStatus.Failed);
            return Task.FromResult(e);
        }

        public Task<WorkflowStep[]> GetScheduledActivitiesAsync()
        {
            var ready = queue.Where(x => x.ETA <= clock.UtcNow).ToList();
            var steps = ready.GroupBy(x => x.ID).Select(x => workflows.FirstOrDefault(w => w.ID == x.Key)).ToArray();
            foreach (var item in ready)
            {
                queue.Remove(item);
            }
            return Task.FromResult(steps);
        }

        public Task<ActivityStep> GetStatusAsync(ActivityStep key)
        {
            var item = list.FirstOrDefault(x => x.ID == key.ID
            && x.ActivityType == key.ActivityType
            && x.KeyHash == key.KeyHash
            && x.Key == key.Key);
            return Task.FromResult(item);
        }

        public Task<WorkflowStep> GetWorkflowAsync(string id)
        {
            return Task.FromResult(workflows.FirstOrDefault(x =>x.ID == id));
        }

        public Task<ActivityStep> InsertActivityAsync(ActivityStep key)
        {
            list.Add(key);
            key.SequenceID = list.Count;
            return Task.FromResult(key);
        }

        public Task<WorkflowStep> InsertAsync(WorkflowStep step)
        {
            if(string.IsNullOrEmpty(step.ID))
            {
                step.ID = Guid.NewGuid().ToString("N");
            } else
            {
                if (workflows.Any(x => x.ID == step.ID))
                    throw new InvalidOperationException();
            }
            workflows.Add(step);
            return Task.FromResult(step);
        }

        public Task<IQueueToken> QueueWorkflowAsync(string id, DateTimeOffset after)
        {
            var qt = new QueueToken(id, after);
            queue.Add(qt);
            return Task.FromResult<IQueueToken>(qt);
        }

        public Task RemoveQueueAsync(IQueueToken token)
        {
            if(token != null)
            {
                var t = token as QueueToken;
                queue.Remove(t);
            }
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ActivityStep key)
        {
            list[(int)key.SequenceID - 1] = key;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(WorkflowStep key)
        {
            int index = workflows.FindIndex(x => x.ID == key.ID);
            workflows[index] = key;
            return Task.CompletedTask;
        }
    }

    public class QueueToken: IQueueToken
    {
        public readonly string ID;

        public readonly DateTimeOffset ETA;

        public QueueToken(string id, DateTimeOffset eta)
        {
            this.ID = id;
            this.ETA = eta;
        }
    }
}
