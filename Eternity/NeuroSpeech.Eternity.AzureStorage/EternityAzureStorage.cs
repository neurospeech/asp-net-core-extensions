using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Queues;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{
    public class EternityAzureStorage : IEternityStorage
    {
        private readonly TableServiceClient TableClient;
        private readonly QueueClient QueueClient;
        private readonly TableClient Activities;
        private readonly TableClient Workflows;
        private readonly BlobContainerClient Locks;

        public EternityAzureStorage(string prefix, string connectionString)
        {
            this.TableClient = new TableServiceClient(connectionString);
            this.QueueClient = new QueueServiceClient(connectionString).GetQueueClient($"{prefix}Workflows");
            var storageClient = new BlobServiceClient(connectionString);
            this.Activities = TableClient.GetTableClient($"{prefix}Activities");
            this.Workflows = TableClient.GetTableClient($"{prefix}Workflows");
            this.Locks = storageClient.GetBlobContainerClient($"{prefix}Locks");

            QueueClient.CreateIfNotExists();
            Activities.CreateIfNotExists();
            Workflows.CreateIfNotExists();
            Locks.CreateIfNotExists();
        }

        public async Task<IEternityLock> AcquireLockAsync(string id, long sequenceId)
        {
            for (int i = 0; i < 30; i++)
            {
                try
                {

                    var lockName = $"{id}-{sequenceId}.lock";
                    var b = Locks.GetBlobClient(lockName);
                    var bc = b.GetBlobLeaseClient();
                    var r = await bc.AcquireAsync(BlobLeaseClient.InfiniteLeaseDuration);
                    return new EternityBlobLock
                    {
                        LeaseID = r.Value.LeaseId,
                        LockName = lockName
                    };
                } catch (Exception ex)
                {

                }
            }
            throw new InvalidOperationException();
        }

        public async Task FreeLockAsync(IEternityLock executionLock)
        {
            var el = executionLock as EternityBlobLock;
            var b = Locks.GetBlobClient(el.LockName);
            var bc = b.GetBlobLeaseClient(el.LeaseID);
            await bc.ReleaseAsync();
        }

        public Task<ActivityStep> GetEventAsync(string id, string eventName)
        {
            throw new NotImplementedException();
        }

        public Task<WorkflowQueueItem[]> GetScheduledActivitiesAsync()
        {
            
            throw new NotImplementedException();
        }

        public Task<ActivityStep> GetStatusAsync(ActivityStep key)
        {
            throw new NotImplementedException();
        }

        public Task<WorkflowStep> GetWorkflowAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<ActivityStep> InsertActivityAsync(ActivityStep key)
        {
            throw new NotImplementedException();
        }

        public Task<WorkflowStep> InsertWorkflowAsync(WorkflowStep step)
        {
            throw new NotImplementedException();
        }

        public Task<string> QueueWorkflowAsync(string id, DateTimeOffset after, string existing = null)
        {
            throw new NotImplementedException();
        }

        public Task RemoveQueueAsync(params string[] token)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(ActivityStep key)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(WorkflowStep key)
        {
            throw new NotImplementedException();
        }
    }
}
