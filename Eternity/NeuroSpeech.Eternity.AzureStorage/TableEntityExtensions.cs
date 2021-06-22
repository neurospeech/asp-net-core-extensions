using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{
    public static class TableEntityExtensions
    {

        public static async Task DeleteAllAsync(this TableClient client, IEnumerable<(string partitionKey, string rowKey)> items)
        {
            while (items.Any())
            {
                var top = items.Take(100).Select(x => new TableTransactionAction(TableTransactionActionType.Delete,
                    new TableEntity(x.partitionKey, x.rowKey), ETag.All
                    ));
                items = items.Skip(100);
                await client.SubmitTransactionAsync(top);
            }
        }
        public static async Task DeleteAllAsync(this TableClient client, string partitionKey)
        {
            var filter = Azure.Data.Tables.TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey}");
            List<TableTransactionAction> actions = new List<TableTransactionAction>();
            while (true)
            {
                await foreach (var step in client.QueryAsync<TableEntity>(filter, select: new string[] { }))
                {
                    actions.Add(new TableTransactionAction(TableTransactionActionType.Delete,
                        new TableEntity(step.PartitionKey, step.RowKey),
                        ETag.All));
                }
                if (actions.Count == 0)
                    break;
                await client.SubmitTransactionAsync(actions);
                actions.Clear();
            }
        }

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
