using StackExchange.Redis;
using Microsoft.Extensions.Options;
using MeLi.UrlShortener.Infrastructure.Configuration;
using MeLi.UrlShortener.Application.Cache;
using System.Text.Json;
using MeLi.UrlShortener.Domain.Entities;
using MeLi.UrlShortener.Infrastructure.Cache.Converters;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace MeLi.UrlShortener.Infrastructure.Cache
{
    public class RedisUrlCache : IUrlCache, IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        private const int DefaultTtlMinutes = 10;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<RedisUrlCache> _logger;
        private readonly IAsyncPolicy _circuitBreakerPolicy;

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

            // Configuración agresiva del Circuit Breaker
            _circuitBreakerPolicy = Policy
                .Handle<RedisConnectionException>()
                .Or<RedisTimeoutException>()
                .Or<RedisException>()
                .CircuitBreakerAsync(
                    // Más fallos en menos tiempo para una detección más rápida
                    exceptionsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromSeconds(5),
                    onBreak: (exception, duration) =>
                    {
                        _logger.LogWarning(
                            "Circuit Breaker abierto por {Duration}s debido a: {Message}",
                            duration.TotalSeconds,
                            exception.Message);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit Breaker reseteado - Redis disponible");
                    });
        }

        public async Task<string> GetLongUrlAsync(string shortCode)
        {
            try
            {
                return await _circuitBreakerPolicy.ExecuteAsync(async () =>
                {
                    var db = _redis.GetDatabase();
                    var value = await db.StringGetAsync(GetKey(shortCode));
                    return value.HasValue ? value.ToString() : null;
                });
            }
            catch (BrokenCircuitException)
            {
                _logger.LogWarning("Circuit Breaker abierto - Saltando Redis para key: {ShortCode}", shortCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accediendo a Redis para key: {ShortCode}", shortCode);
                return null;
            }
        }

        public async Task SetUrlAsync(string shortCode, string longUrl, TimeSpan? ttl = null)
        {
            try
            {
                await _circuitBreakerPolicy.ExecuteAsync(async () =>
                {
                    var db = _redis.GetDatabase();
                    await db.StringSetAsync(
                        GetKey(shortCode),
                        longUrl,
                        ttl ?? TimeSpan.FromMinutes(DefaultTtlMinutes)
                    );
                    return true;
                });
            }
            catch (BrokenCircuitException)
            {
                _logger.LogWarning("Circuit Breaker abierto - No se pudo guardar en Redis: {ShortCode}", shortCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando en Redis: {ShortCode}", shortCode);
            }
        }

        public async Task DeleteLongUrlAsync(string shortCode)
        {
            try
            {
                await _circuitBreakerPolicy.ExecuteAsync(async () =>
                {
                    var db = _redis.GetDatabase();
                    await db.KeyDeleteAsync(GetKey(shortCode));
                    return true;
                });
            }
            catch (BrokenCircuitException)
            {
                _logger.LogWarning("Circuit Breaker abierto - No se pudo eliminar de Redis: {ShortCode}", shortCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando de Redis: {ShortCode}", shortCode);
            }
        }

        public async Task<T> GetAsync<T>(string cacheKey)
        {
            try
            {
                return await _circuitBreakerPolicy.ExecuteAsync(async () =>
                {
                    var db = _redis.GetDatabase();
                    var value = await db.StringGetAsync(cacheKey);
                    return value.HasValue
                        ? JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions)
                        : default;
                });
            }
            catch (BrokenCircuitException)
            {
                _logger.LogWarning("Circuit Breaker abierto - Saltando Redis para key: {Key}", cacheKey);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accediendo a Redis para key: {Key}", cacheKey);
                return default;
            }
        }

        public async Task SetAsync<T>(string cacheKey, T entity, TimeSpan? timeSpan = null)
        {
            try
            {
                await _circuitBreakerPolicy.ExecuteAsync(async () =>
                {
                    var db = _redis.GetDatabase();
                    var jsonValue = JsonSerializer.Serialize(entity, _jsonOptions);
                    await db.StringSetAsync(
                        cacheKey,
                        jsonValue,
                        timeSpan ?? TimeSpan.FromMinutes(DefaultTtlMinutes)
                    );
                    return true;
                });
            }
            catch (BrokenCircuitException)
            {
                _logger.LogWarning("Circuit Breaker abierto - No se pudo guardar en Redis: {Key}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando en Redis: {Key}", cacheKey);
            }
        }

        private static string GetKey(string shortCode) => $"url:{shortCode}";

        public void Dispose()
        {
            _redis?.Dispose();
        }
    }
}