using StackExchange.Redis;
using Microsoft.Extensions.Options;
using MeLi.UrlShortener.Infrastructure.Configuration;
using MeLi.UrlShortener.Application.Cache;
using System.Text.Json;
using MeLi.UrlShortener.Domain.Entities;
using MeLi.UrlShortener.Infrastructure.Cache.Converters;
using MeLi.UrlShortener.Infrastructure.Resilience;
using Microsoft.Extensions.Logging;

namespace MeLi.UrlShortener.Infrastructure.Cache
{
    public class RedisUrlCache : IUrlCache, IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        private const int DefaultTtlMinutes = 10;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<RedisUrlCache> _logger;

        public RedisUrlCache(IOptions<RedisSettings> settings, ILogger<RedisUrlCache> logger)
        {
            if (settings?.Value == null)
                throw new ArgumentNullException(nameof(settings));

            _redis = ConnectionMultiplexer.Connect(settings.Value.ConnectionString);
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false,
                PropertyNamingPolicy = null
            };
            _jsonOptions.Converters.Add(new PrivateSetterJsonConverter<UrlAnalytics>());
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GetLongUrlAsync(string shortCode)
        {
            return await ResiliencePolicies
                .CreateAsyncPolicy<string>()
                .ExecuteAsync(async () => {
                    var db = _redis.GetDatabase();
                    var value = await db.StringGetAsync(GetKey(shortCode));
                    return value.HasValue ? value.ToString() : null;
                });
        }

        public async Task SetUrlAsync(string shortCode, string longUrl, TimeSpan? ttl = null)
        {
            await ResiliencePolicies
                .CreateAsyncPolicy<string>()
                .ExecuteAsync(async () =>
                {
                    var db = _redis.GetDatabase();
                    await db.StringSetAsync(
                        GetKey(shortCode),
                        longUrl,
                        ttl ?? TimeSpan.FromMinutes(DefaultTtlMinutes)
                    );

                    return longUrl;
                });
        }

        private static string GetKey(string shortCode) => $"url:{shortCode}";

        public void Dispose()
        {
            _redis?.Dispose();
        }

        public async Task DeleteLongUrlAsync(string shortCode)
        {
            await ResiliencePolicies
                .CreateAsyncPolicy()
                .ExecuteAsync(async () => {
                    var db = _redis.GetDatabase();
                    await db.KeyDeleteAsync(GetKey(shortCode));
                    return string.Empty;
                });
        }

        public async Task<T> GetAsync<T>(string cacheKey)
        {
            return await ResiliencePolicies
                .CreateAsyncPolicy<T>()
                .ExecuteAsync(async () => {
                    var db = _redis.GetDatabase();
                    var value = await db.StringGetAsync(cacheKey);
                    return value.HasValue
                        ? JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions)
                        : default;
                });
        }

        public async Task SetAsync<T>(string cacheKey, T entity, TimeSpan? timeSpan = null)
        {
            await ResiliencePolicies
                .CreateAsyncPolicy()
                .ExecuteAsync(async () => {
                    var db = _redis.GetDatabase();
                    var jsonValue = JsonSerializer.Serialize(entity, _jsonOptions);
                    await db.StringSetAsync(
                        cacheKey,
                        jsonValue,
                        timeSpan ?? TimeSpan.FromMinutes(DefaultTtlMinutes)
                    );
                });
        }
    }
}