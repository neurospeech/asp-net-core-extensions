using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity.Tests.Mocks
{
    public class MockDatabase
    {
        private List<ActivityStep> list = new List<ActivityStep>();
        private List<WorkflowStep> workflows = new List<WorkflowStep>();

        internal Task<ActivityStep> InsertAsync(ActivityStep key)
        {
            lock (this)
            {
                list.Add(key);
                key.SequenceID = list.Count;
            }
            return Task.FromResult(key);
        }

        internal Task UpdateAsync(ActivityStep key)
        {
            lock (this)
            {
                list[(int)key.SequenceID-1] = key;
            }
            return Task.CompletedTask;
        }

        internal Task<WorkflowStep> SearchAsync(string id, ActivityType workflow)
        {
            lock (this)
            {
                return Task.FromResult(workflows
                    .FirstOrDefault(x => x.ID == id));
            }
        }

        internal Task<ActivityStep> SearchAsync(string id, ActivityType activityType, string keyHash, string key)
        {
            lock (this)
            {
                return Task.FromResult(list.FirstOrDefault(x => x.ID == id 
                && x.ActivityType == activityType 
                && x.KeyHash == keyHash 
                && x.Key == key));
            }
        }

        internal WorkflowStep[] GetReadyAsync(DateTimeOffset utcNow)
        {
            var steps = new List<WorkflowStep>();
            foreach(var item in list.Where(x => x.ETA <= utcNow).GroupBy(x => x.ID))
            {
                var workflow = workflows
                    .FirstOrDefault(x => x.ID == item.Key);
                steps.Add(workflow);
                if (steps.Count == 0)
                    break;
            }
            return steps.ToArray();
            
        }

        internal ActivityStep GetEventAsync(string id, string eventName)
        {
            return list.FirstOrDefault(x => x.ID == id
                && x.ActivityType == ActivityType.Event
                && x.Status != ActivityStatus.Completed
                && x.Status != ActivityStatus.Failed);
        }

        internal Task<WorkflowStep> InsertAsync(WorkflowStep step)
        {
            lock (this)
            {
                if (string.IsNullOrWhiteSpace(step.ID))
                {
                    step.ID = Guid.NewGuid().ToString("N");
                } else
                {
                    if (workflows.Any(x => x.ID == step.ID))
                        throw new ArgumentException($"Workflow for ID {step.ID} already exists");
                }
                workflows.Add(step);
                return Task.FromResult(step);
            }
        }

        internal Task UpdateAsync(WorkflowStep key)
        {
            lock (this)
            {
                var index = workflows.FindIndex(x => x.ID == key.ID);
                workflows[index] = key;
            }
            return Task.CompletedTask;
        }
    }
}
