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
            string key,
            Func<ICacheEntry, Task<T>> factory)
        {
            lock (lockObject)
            {
                return cache.GetOrCreate<Task<T>>(key, (entry) => {
                    return factory(entry);
                });
            }
        }
    }
}
