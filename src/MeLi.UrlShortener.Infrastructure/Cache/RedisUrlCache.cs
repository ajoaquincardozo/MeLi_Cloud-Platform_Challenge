﻿using StackExchange.Redis;
using Microsoft.Extensions.Options;
using MeLi.UrlShortener.Infrastructure.Configuration;
using MeLi.UrlShortener.Application.Cache;

namespace MeLi.UrlShortener.Infrastructure.Cache
{
    public class RedisUrlCache : IUrlCache, IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        private const int DefaultTtlMinutes = 10;

        public RedisUrlCache(IOptions<RedisSettings> settings)
        {
            if (settings?.Value == null)
                throw new ArgumentNullException(nameof(settings));

            _redis = ConnectionMultiplexer.Connect(settings.Value.ConnectionString);
        }

        public async Task<string?> GetLongUrlAsync(string shortCode)
        {
            try
            {
                var db = _redis.GetDatabase();
                var value = await db.StringGetAsync(GetKey(shortCode));
                return value.HasValue ? value.ToString() : null;
            }
            catch (Exception ex)
            {
                // Log error but don't throw - let the system fallback to MongoDB
                return null;
            }
        }

        public async Task SetUrlAsync(string shortCode, string longUrl, TimeSpan? ttl = null)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.StringSetAsync(
                    GetKey(shortCode),
                    longUrl,
                    ttl ?? TimeSpan.FromMinutes(DefaultTtlMinutes)
                );
            }
            catch (Exception ex)
            {
                // Log error but don't throw - the system can continue without cache
            }
        }

        private static string GetKey(string shortCode) => $"url:{shortCode}";

        public void Dispose()
        {
            _redis?.Dispose();
        }

        public async Task DeleteLongUrlAsync(string shortCode)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(GetKey(shortCode));
        }
    }
}