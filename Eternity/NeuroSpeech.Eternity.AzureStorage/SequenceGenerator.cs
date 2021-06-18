using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NeuroSpeech.Eternity
{
    public static class SequenceGenerator
    {
        public static string ToStringWithZeros(this long n)
        {
            return $"{n:d20}";
        }

        public static async Task<long> NewSequenceIDAsync(
            this TableClient client, 
            string partitionKey,
            string rowKey)
        {
            long id = 1;
            while (true)
            {
                try
                {
                    var en = client.QueryAsync<TableEntity>(x => x.PartitionKey == partitionKey && x.RowKey == rowKey).GetAsyncEnumerator();
                    if (!await en.MoveNextAsync())
                    {
                        await client.AddEntityAsync<TableEntity>(new TableEntity(partitionKey, rowKey) {
                            { "SequenceID", (long)1 }
                        });
                        break;
                    }
                    var item = en.Current;
                    id = item.GetInt64("SequenceID").GetValueOrDefault() + 1;
                    item["SequenceID"] = id;

                    await client.UpdateEntityAsync(item, item.ETag, TableUpdateMode.Replace);
                    break;
                }
                catch (RequestFailedException ex)
                {
                    if (ex.Status == 412)
                    {
                        continue;
                    }
                    throw;
                }
            }
            return id;
        }

    }
}
