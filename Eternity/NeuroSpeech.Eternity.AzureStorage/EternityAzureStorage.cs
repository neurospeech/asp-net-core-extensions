using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{
    public class EternityAzureStorage : IEternityStorage
    {
        private readonly TableServiceClient TableClient;
        // private readonly QueueClient QueueClient;
        private readonly TableClient Activities;
        private readonly TableClient Workflows;
        private readonly TableClient ActivityQueue;
        private readonly BlobContainerClient Locks;

        

        public EternityAzureStorage(string prefix, string connectionString)
        {
            this.TableClient = new TableServiceClient(connectionString);
            // this.QueueClient = new QueueServiceClient(connectionString).GetQueueClient($"{prefix}Workflows".ToLower());
            var storageClient = new BlobServiceClient(connectionString);
            this.Activities = TableClient.GetTableClient($"{prefix}Activities".ToLower());
            this.Workflows = TableClient.GetTableClient($"{prefix}Workflows".ToLower());
            this.ActivityQueue = TableClient.GetTableClient($"{prefix}Queue".ToLower());
            this.Locks = storageClient.GetBlobContainerClient($"{prefix}Locks".ToLower());

            // QueueClient.CreateIfNotExists();
            try
            {
                Activities.CreateIfNotExists();
            }
            catch { }
            try {
                Workflows.CreateIfNotExists();
            }
            catch { }
            try { ActivityQueue.CreateIfNotExists(); } catch { }
            try
            {
                Locks.CreateIfNotExists();
            } catch { }
        }

        public async Task<IEternityLock> AcquireLockAsync(string id, long sequenceId)
        {
            for (int i = 0; i < 30; i++)
            {
                try
                {

                    var lockName = $"{id}-{sequenceId}.lock";
                    var b = Locks.GetBlobClient(lockName);
                    if(!(await b.ExistsAsync()))
                    {
                        await b.UploadAsync(new MemoryStream(new byte[] { 1, 2, 3 }));
                    }
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

        public async Task<ActivityStep> GetEventAsync(string id, string eventName)
        {
            var filter = Azure.Data.Tables.TableClient.CreateQueryFilter($"PartitionKey eq {id} and RowKey eq {eventName}");
            string keyHash = null;
            string key = null;
            await foreach (var e in Activities.QueryAsync<TableEntity>(filter))
            {
                key = e.GetString("Key");
                keyHash = e.GetString("KeyHash");
            }
            if (key == null)
            {
                return null;
            }
            filter = Azure.Data.Tables.TableClient.CreateQueryFilter($"PartitionKey eq {id} and RowKey eq {keyHash} and Key eq {key}");
            await foreach(var e in Activities.QueryAsync<TableEntity>(filter))
            {
                return e.ToObject<ActivityStep>();
            }
            return null;
        }

        public async Task<WorkflowQueueItem[]> GetScheduledActivitiesAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var nowTicks = now.UtcTicks;

            var locked = now.AddSeconds(60).UtcTicks;

            var list = new List<TableEntity>();
            await foreach(var item in ActivityQueue.QueryAsync<TableEntity>())
            {
                var eta = item.GetDateTimeOffset("ETA").Value.UtcTicks;
                if(eta > nowTicks)
                {
                    break;
                }
                var entityLocked = item.GetInt64("Locked").GetValueOrDefault();
                if (entityLocked != 0 && entityLocked < locked)
                {
                    continue;
                }
                item["Locked"] = locked;
                try
                {
                    await ActivityQueue.UpdateEntityAsync(item, item.ETag);
                    list.Add(item);
                    if (list.Count == 32)
                        break;
                }
                catch (RequestFailedException re)
                {
                    if (re.Status == 419)
                        continue;
                    throw;
                }
            }
            return list.Select(x => new WorkflowQueueItem {
                ID = x.GetString("Message"),
                QueueToken = $"{x.PartitionKey},{x.RowKey},{x.ETag}"
            }).ToArray();
            //var messages = await QueueClient.ReceiveMessagesAsync(32, TimeSpan.FromDays(1));
            //var data = messages.Value;
            //var items = new WorkflowQueueItem[data.Length];
            //for (int i = 0; i < items.Length; i++)
            //{
            //    var item = data[i];
            //    items[i] = new WorkflowQueueItem { 
            //        ID = item.Body.ToString(),
            //        QueueToken = $"{item.MessageId},{item.PopReceipt}"
            //    };
            //}
            //return items;

        }

        public async Task<ActivityStep> GetStatusAsync(ActivityStep key)
        {
            var filter = Azure.Data.Tables.TableClient.CreateQueryFilter($"PartitionKey eq {key.ID} and RowKey eq {key.KeyHash} and Key eq {key.Key}");
            await foreach (var e in Activities.QueryAsync<TableEntity>(filter)) {
                return e.ToObject<ActivityStep>();
            }
            return null;
        }

        public async Task<WorkflowStep> GetWorkflowAsync(string id)
        {
            await foreach(var e in Workflows.QueryAsync<TableEntity>(x => x.PartitionKey == id && x.RowKey == "1"))
            {
                return e.ToObject<WorkflowStep>();
            }
            return null;
        }

        public async Task<ActivityStep> InsertActivityAsync(ActivityStep key)
        {
            // generate new id...
            long id = await Activities.NewSequenceIDAsync(key.ID, "ID");
            key.SequenceID = id;
            await Activities.UpsertEntityAsync(key.ToTableEntity(key.ID, key.KeyHash));

            // last active event waiting must be added with eventName
            if(key.ActivityType == ActivityType.Event)
            {
                string[] eventNames = JsonSerializer.Deserialize<string[]>(key.Parameters);
                foreach(var name in eventNames)
                {
                    await Activities.UpsertEntityAsync(new TableEntity(key.ID, name)
                    {
                        { "Key", key.Key },
                        { "KeyHash", key.KeyHash }                         
                    }, TableUpdateMode.Replace);
                }
            }

            return key;
        }

        public async Task<WorkflowStep> InsertWorkflowAsync(WorkflowStep step)
        {
            await UpdateAsync(step);
            return step;
        }

        public async Task<string> QueueWorkflowAsync(string id, DateTimeOffset after, string existing = null)
        {
            if (existing != null)
            {
                await RemoveQueueMessageAsync(existing);
            }
            var utc = after.UtcDateTime;
            var day = utc.Date.Ticks.ToStringWithZeros();
            var time = utc.TimeOfDay.Ticks.ToStringWithZeros();
            for (long sid = DateTime.UtcNow.Ticks; sid <= long.MaxValue; sid++)
            {
                try
                {
                    var key = $"{time}-{sid.ToStringWithZeros()}";
                    var r = await ActivityQueue.AddEntityAsync(new TableEntity(day, key) {
                        { "Message", id },
                        { "ETA", after }
                    });
                    return $"{day},{key},{r.Headers.ETag}";
                }
                catch (RequestFailedException ex)
                {
                    if (ex.Status == 409)
                        continue;
                }
            }
            throw new UnauthorizedAccessException();
        }

        public Task RemoveQueueAsync(params string[] tokens)
        {
            return Task.WhenAll(tokens.Select(RemoveQueueMessageAsync));
        }

        private async Task RemoveQueueMessageAsync(string id)
        {
            var tokens = id.Split(',');
            var pk = tokens[0];
            var rk = tokens[1];
            var etag = tokens[2];
            await ActivityQueue.DeleteEntityAsync(pk, rk, new ETag(etag));
        }

        public Task UpdateAsync(ActivityStep key)
        {
            return Activities.UpsertEntityAsync(key.ToTableEntity(key.ID, key.KeyHash));
        }

        public Task UpdateAsync(WorkflowStep key)
        {
            return Workflows.UpsertEntityAsync(key.ToTableEntity(key.ID, "1"), TableUpdateMode.Replace);
        }
    }

    public static class TableEntityExtensions
    {
        public static TableEntity ToTableEntity<T>(this T item, string partitionKey, string rowKey)
            where T : class, new()
        {
            Type type = typeof(T);
            var entity = new TableEntity(partitionKey, rowKey);
            foreach (var property in type.GetProperties())
            {
                if (property.CanRead && property.CanWrite)
                {
                    Type propertyType = property.PropertyType;
                    if (propertyType.IsEnum)
                    {
                        entity.Add(property.Name, propertyType.GetEnumName(property.GetValue(item)));
                        continue;
                    }
                    entity.Add(property.Name, property.GetValue(item));
                }
            }
            return entity;
        }

        public static T ToObject<T>(this TableEntity entity)
        {
            Type type = typeof(T);
            var result = Activator.CreateInstance<T>();
            foreach (var property in type.GetProperties())
            {
                if (property.CanRead && property.CanWrite)
                {
                    if (entity.TryGetValue(property.Name, out var text))
                    {
                        Type propertyType = property.PropertyType;
                        if (propertyType.IsEnum)
                        {
                            property.SetValue(result, Enum.Parse(propertyType, text.ToString()));
                            continue;
                        }
                        property.SetValue(result, text);
                    }
                }
            }
            return result;
        }
    }
}
