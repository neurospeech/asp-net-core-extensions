using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching
{
    /// <summary>
    /// 
    /// </summary>
    [DIRegister(ServiceLifetime.Singleton)]
    public class AppCache<T>
    {
        private IMemoryCache cache;
        private string prefix;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cache"></param>
        public AppCache(IMemoryCache cache)
        {
            this.cache = cache;
            this.prefix = typeof(T).FullName + ":";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public T GetOrCreate(object key, Func<ICacheEntry, T> factory)
        {
            return cache.GetOrCreate(prefix + key, ci =>
            {
                // by default minimum expiration is one minute
                ci.SetSlidingExpiration(TimeSpan.FromMinutes(1));
                return factory(ci);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public Task<T> GetOrCreateAsync(object key, Func<ICacheEntry, Task<T>> factory)
        {
            return AtomicGetOrCreateAsync(prefix + key, ci =>
            {
                // by default minimum expiration is one minute
                ci.SetSlidingExpiration(TimeSpan.FromMinutes(1));
                return factory(ci);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            cache.Remove(prefix + key);
        }

        private object lockObject = new object();

        private Task<T> AtomicGetOrCreateAsync(
            string key,
            Func<ICacheEntry, Task<T>> factory)
        {
            return cache.GetOrCreate<Task<T>>(key, (entry) =>
            {
                lock (lockObject)
                {
                    Func<ICacheEntry, Task<T>> fx = async (e2) => {
                        try
                        {
                            return await factory(e2);
                        }
                        catch
                        {
                            // forcing getting out of lock...
                            await Task.Delay(10);
                            // it is stale... 
                            lock (lockObject)
                            {
                                cache.Remove(key);
                            }

                            // awaiter must know that 
                            // first request was unsuccessful 
                            // and must try again...
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
