using Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;

namespace Infrastructure.Services
{
    public sealed class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisCacheService> _logger;
        private static readonly Dictionary<string, RefCountedLock> Locks = new();
        private static readonly object LockObj = new();

        public RedisCacheService(
            IDistributedCache distributedCache,
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisCacheService> logger)
        {
            _distributedCache = distributedCache;
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
                if (cachedValue is null)
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(cachedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache with key {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value);
                var options = new DistributedCacheEntryOptions();

                if (expiration.HasValue)
                {
                    options.AbsoluteExpirationRelativeToNow = expiration.Value;
                }
                else
                {
                    options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                }

                await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
                await TrackKeyAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache with key {Key}", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
                await UntrackKeyAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache with key {Key}", key);
            }
        }

        public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _connectionMultiplexer.GetDatabase();
                var setKey = $"prefix_keys:{prefix}";
                var keys = await db.SetMembersAsync(setKey);

                if (keys.Length > 0)
                {
                    var tasks = keys.Select(key => db.KeyDeleteAsync(key.ToString())).ToArray();
                    await Task.WhenAll(tasks);
                }

                await db.KeyDeleteAsync(setKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache with prefix {Prefix}", prefix);
            }
        }

        public async Task<T?> GetOrAddAsync<T>(
            string key,
            Func<Task<T?>> factory,
            TimeSpan? expiration = null,
            CancellationToken cancellationToken = default)
        {
            var cachedValue = await GetAsync<T>(key, cancellationToken);
            if (cachedValue is not null)
            {
                return cachedValue;
            }

            RefCountedLock myLock;
            lock (LockObj)
            {
                if (Locks.TryGetValue(key, out var existing))
                {
                    existing.RefCount++;
                    myLock = existing;
                }
                else
                {
                    myLock = new RefCountedLock();
                    Locks[key] = myLock;
                }
            }

            bool acquired = false;
            try
            {
                await myLock.Semaphore.WaitAsync(cancellationToken);
                acquired = true;

                cachedValue = await GetAsync<T>(key, cancellationToken);
                if (cachedValue is not null)
                {
                    return cachedValue;
                }

                var freshValue = await factory();

                if (freshValue is not null)
                {
                    await SetAsync(key, freshValue, expiration, cancellationToken);
                }

                return freshValue;
            }
            finally
            {
                if (acquired)
                {
                    myLock.Semaphore.Release();
                }

                lock (LockObj)
                {
                    myLock.RefCount--;
                    if (myLock.RefCount == 0)
                    {
                        Locks.Remove(key);
                        myLock.Semaphore.Dispose();
                    }
                }
            }
        }

        private async Task TrackKeyAsync(string key)
        {
            var prefix = GetPrefix(key);
            if (prefix is not null)
            {
                try
                {
                    var db = _connectionMultiplexer.GetDatabase();
                    await db.SetAddAsync($"prefix_keys:{prefix}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error tracking key {Key} for prefix {Prefix}", key, prefix);
                }
            }
        }

        private async Task UntrackKeyAsync(string key)
        {
            var prefix = GetPrefix(key);
            if (prefix is not null)
            {
                try
                {
                    var db = _connectionMultiplexer.GetDatabase();
                    await db.SetRemoveAsync($"prefix_keys:{prefix}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error untracking key {Key} for prefix {Prefix}", key, prefix);
                }
            }
        }

        private static string? GetPrefix(string key)
        {
            var colonIndex = key.IndexOf(':');
            return colonIndex > 0 ? key.Substring(0, colonIndex + 1) : null;
        }

        private sealed class RefCountedLock
        {
            public SemaphoreSlim Semaphore { get; } = new(1, 1);
            public int RefCount { get; set; } = 1;
        }
    }
}
