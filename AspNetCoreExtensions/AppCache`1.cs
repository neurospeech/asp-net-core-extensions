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
    [DIRegister(Extensions.DependencyInjection.ServiceLifetime.Singleton)]
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
            this.prefix = typeof(T).Name + ":";
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
            return cache.GetOrCreateAsync(prefix + key, ci =>
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
        public Task<T> GetOrCreateLargeTTLAsync(object key, Func<ICacheEntry, Task<T>> factory)
        {
            return cache.GetOrCreateAsync(prefix + key, ci =>
            {
                // by default minimum expiration is one minute
                ci.SetSlidingExpiration(TimeSpan.FromMinutes(60));
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
    }
}
