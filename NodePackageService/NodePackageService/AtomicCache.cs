using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuroSpeech.Internal
{
    internal class AtomicCache<T> : IDisposable
    {

        public class CacheEntry
        {
            public string Key { get; }

            public CacheEntry(string key)
            {
                this.Key = key;
            }

            public List<Action<CacheEntry>> EvictionCallbacks = new List<Action<CacheEntry>>();

            public Task<T> Value { get; set; }
            public DateTimeOffset? AbsoluteExpiration { get; set; }
            public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
            public TimeSpan? SlidingExpiration { get; set; }

            public void Evict()
            {
                foreach(var c in EvictionCallbacks)
                {
                    c(this);
                }
            }

        }

        private ConcurrentDictionary<string, CacheEntry> cache = new ConcurrentDictionary<string, CacheEntry>();
        private bool _disposed = false;

        private TimeSpan InvalidationTimeSpan = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Maximum objects to store in cache, cannot be less than 100. Setting this value will not clear objects
        /// immediately
        /// </summary>
        public int MaxObjects
        {
            get => _maxObjects; set
            {
                if (value < 100)
                {
                    throw new ArgumentException($"{nameof(MaxObjects)} cannot be less than 100");
                }
                _maxObjects = value;
            }
        }
        public AtomicCache()
        {
        }

        public async Task<T> GetAsync(string key, Func<CacheEntry, Task<T>> factory)
        {
            CheckDisposed();
            bool created = false;
            var e = cache.GetOrAdd(key, k =>
            {
                lock (this)
                {
                    return cache.GetOrAdd(key, k2 =>
                    {
                        CheckDisposed();
                        var entry = new CacheEntry(key);
                        entry.Value = factory(entry);
                        created = true;
                        if (entry.AbsoluteExpiration == null
                            && entry.AbsoluteExpirationRelativeToNow == null
                            && entry.SlidingExpiration == null)
                        {
                            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                        }
                        return entry;
                    });
                }
            });
            if (created)
            {
                QueueClear();
            }
            var now = DateTime.UtcNow;
            if (e.AbsoluteExpiration != null)
            {
                if (e.AbsoluteExpiration.Value < now)
                {
                    cache.TryRemove(key, out var old);
                    old.Evict();
                    return await GetAsync(key, factory);
                }
            }
            if (e.AbsoluteExpirationRelativeToNow != null)
            {
                e.AbsoluteExpiration = now.Add(e.AbsoluteExpirationRelativeToNow.Value);
                e.AbsoluteExpirationRelativeToNow = null;
            }
            if (e.SlidingExpiration != null)
            {
                e.AbsoluteExpiration = now.Add(e.SlidingExpiration.Value);
            }
            try
            {
                return await e.Value;
            }
            catch (Exception)
            {
                cache.TryRemove(key, out var old);
                old.Evict();
                throw;
            }
        }

        private DateTime lastCheck = DateTime.MinValue;
        private int _maxObjects = 1000;

        private void QueueClear()
        {
            var now = DateTime.UtcNow;
            lock (this)
            {
                if (lastCheck.Add(InvalidationTimeSpan) < now)
                {
                    Task.Factory.StartNew(() => Clear(), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                    lastCheck = now;
                }
            }
        }

        public void Dispose()
        {
            _disposed = true;
            cache.Clear();
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("this");
            }
        }
        private void Clear()
        {
            var now = DateTime.UtcNow;
            var values = cache.Values
                .OrderByDescending(x => x.AbsoluteExpiration)
                .ToList();

            foreach (var value in values)
            {
                if (cache.Count > this.MaxObjects
                    ||
                    value.AbsoluteExpiration == null
                    || value.AbsoluteExpiration.Value < now)
                {
                    cache.TryRemove(value.Key, out var old);
                    old.Evict();
                }
            }
        }

        public bool Remove(string key)
        {
            return cache.TryRemove(key, out var ce);
        }
    }
}
