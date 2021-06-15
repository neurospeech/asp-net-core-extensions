using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity.Tests.Mocks
{
    public class MockDatabase
    {
        private List<ActivityStep> list = new List<ActivityStep>();

        internal Task<ActivityStep> InsertAsync(ActivityStep key)
        {
            lock (this)
            {
                key.SequenceID = list.Count;
                list.Add(key);
            }
            return Task.FromResult(key);
        }

        internal Task UpdateAsync(ActivityStep key)
        {
            lock (this)
            {
                list[(int)key.SequenceID] = key;
            }
            return Task.CompletedTask;
        }

        internal Task<ActivityStep> SearchAsync(string id, ActivityType workflow)
        {
            lock (this)
            {
                return Task.FromResult(list.FirstOrDefault(x => x.ID == id && x.ActivityType == workflow));
            }
        }

        internal Task<ActivityStep> SearchAsync(string id, ActivityType activityType, string parametersHash, string parameters)
        {
            lock (this)
            {
                return Task.FromResult(list.FirstOrDefault(x => x.ID == id 
                && x.ActivityType == activityType 
                && x.ParametersHash == parametersHash 
                && x.Parameters == parameters));
            }
        }

        internal ActivityStep[] GetReadyAsync(DateTimeOffset utcNow)
        {
            return list.Where(x => x.ETA <= utcNow).Take(10).ToArray();
        }

        internal ActivityStep GetEventAsync(string id, string eventName)
        {
            return list.FirstOrDefault(x => x.ID == id
                && x.ActivityType == ActivityType.Event
                && x.Status != ActivityStatus.Completed
                && x.Status != ActivityStatus.Failed);
        }
    }
}
