using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Memory
{
    public static class AtomicMemoryCacheExtensions
    {

        private static Object lockObject = new object();

        public static Task<T> AtomicGetOrCreateAsync<T>(
            this IMemoryCache cache,
            object key,
            Func<ICacheEntry, Task<T>> factory)
        {
            return cache.GetOrCreate<Task<T>>(key, (entry) =>
            {
                lock (lockObject)
                {
                    Func<ICacheEntry, Task<T>> fx = async (e2) => {
                        try {
                            return await factory(e2);
                        } catch
                        {
                            // it is stale... 
                            cache.Remove(key);
                            throw;
                        }
                    };
                    return cache.GetOrCreate<Task<T>>(key, (e1) =>
                    {
                        return fx(e1);
                    });
                }
            });
        }
    }
}
