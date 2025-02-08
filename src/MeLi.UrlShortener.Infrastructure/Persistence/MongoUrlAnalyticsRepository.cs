using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MeLi.UrlShortener.Domain.Entities;
using MeLi.UrlShortener.Domain.Interfaces;
using MeLi.UrlShortener.Infrastructure.Configuration;
using MongoDB.Bson;

namespace MeLi.UrlShortener.Infrastructure.Persistence
{
    public class MongoUrlAnalyticsRepository : IUrlAnalyticsRepository, IDisposable
    {
        private readonly IMongoCollection<UrlAnalytics> _collection;
        private readonly ConcurrentQueue<AccessRecord> _pendingWrites;
        private readonly Timer _batchWriteTimer;
        private const int BatchWriteIntervalMs = 1000; // 1 second
        private const int MaxBatchSize = 1000;
        private bool _disposed;

        public MongoUrlAnalyticsRepository(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _collection = database.GetCollection<UrlAnalytics>(settings.Value.UrlAnalyticsCollectionName);

            _pendingWrites = new ConcurrentQueue<AccessRecord>();
            _batchWriteTimer = new Timer(
                async _ => await ProcessBatchWritesAsync(),
                null,
                BatchWriteIntervalMs,
                BatchWriteIntervalMs);
        }

        public async Task RecordAccessAsync(string shortCode, DateTime accessTime)
        {
            // Queue the access record for batch processing
            _pendingWrites.Enqueue(new AccessRecord(shortCode, accessTime));

            // Fire-and-forget immediate update of total count
            _ = IncrementTotalCountAsync(shortCode);
        }

        private async Task ProcessBatchWritesAsync()
        {
            if (_disposed) return;

            try
            {
                var batch = new List<AccessRecord>();
                while (_pendingWrites.TryDequeue(out var record) && batch.Count < MaxBatchSize)
                {
                    batch.Add(record);
                }

                if (batch.Any())
                {
                    var operations = new List<WriteModel<UrlAnalytics>>();
                    var groupedRecords = batch.GroupBy(x => x.ShortCode);

                    foreach (var group in groupedRecords)
                    {
                        var shortCode = group.Key;
                        var updates = CreateBatchUpdates(group);
                        operations.Add(updates);
                    }

                    await _collection.BulkWriteAsync(operations);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - we don't want to break the application for analytics
            }
        }

        private WriteModel<UrlAnalytics> CreateBatchUpdates(IGrouping<string, AccessRecord> group)
        {
            var shortCode = group.Key;
            var date = group.First().AccessTime.Date;

            // Agrupar hits por hora
            var hitsByHour = group.GroupBy(x => x.AccessTime.Hour)
                                .ToDictionary(x => x.Key, x => x.Count());

            // Crear el filtro para el documento
            var filter = Builders<UrlAnalytics>.Filter.And(
                Builders<UrlAnalytics>.Filter.Eq(x => x.ShortCode, shortCode),
                Builders<UrlAnalytics>.Filter.ElemMatch(x => x.DailyAccesses, d => d.Date == date)
            );

            // Preparar la actualización inicial
            var update = Builders<UrlAnalytics>.Update
                .Set(x => x.LastCalculatedAt, DateTime.UtcNow)
                .Inc(x => x.TotalAccessCount, group.Count());

            // Si el día no existe, inicializarlo con ceros
            var initialDay = new DailyAccess
            {
                Date = date,
                HourlyHits = new int[24],
                TotalDayHits = 0
            };

            // Actualizar los hits para cada hora
            foreach (var hourHits in hitsByHour)
            {
                var hour = hourHits.Key;
                var hits = hourHits.Value;
                initialDay.HourlyHits[hour] = hits;
                initialDay.TotalDayHits += hits;
            }

            // Construir la actualización final usando una expresión condicional
            var finalUpdate = Builders<UrlAnalytics>.Update.Pipeline(new[]
{
    new BsonDocument("$addFields", new BsonDocument
    {
        {
            "dailyAccesses", new BsonDocument("$cond", new BsonDocument
            {
                { "if", new BsonDocument("$ne", new BsonArray { "$dailyAccesses", BsonNull.Value }) }, // Verifica si $dailyAccesses no es null
                {
                    "then", new BsonDocument("$cond", new BsonDocument
                    {
                        {
                            "if", new BsonDocument("$in", new BsonArray
                            {
                                date,
                                new BsonDocument("$map", new BsonDocument // Corrección: $map dentro de $in
                                {
                                    { "input", "$dailyAccesses" },
                                    { "as", "item" },
                                    { "in", "$$item.date" } // Extrae el campo 'date' de cada objeto
                                })
                            })
                        },
                        {
                            "then", new BsonDocument("$map", new BsonDocument
                            {
                                { "input", "$dailyAccesses" },
                                { "as", "day" },
                                {
                                    "in", new BsonDocument("$cond", new BsonDocument
                                    {
                                        { "if", new BsonDocument("$eq", new BsonArray { "$$day.date", date }) },
                                        {
                                            "then", new BsonDocument
                                            {
                                                { "date", "$$day.date" },
                                                { "hourlyHits", new BsonArray(initialDay.HourlyHits) },
                                                { "totalDayHits", initialDay.TotalDayHits }
                                            }
                                        },
                                        { "else", "$$day" }
                                    })
                                }
                            })
                        },
                        {
                            "else", new BsonDocument("$concatArrays", new BsonArray
                            {
                                "$dailyAccesses",
                                new BsonArray
                                {
                                    new BsonDocument
                                    {
                                        { "date", date },
                                        { "hourlyHits", new BsonArray(initialDay.HourlyHits) },
                                        { "totalDayHits", initialDay.TotalDayHits }
                                    }
                                }
                            })
                        }
                    })
                },
                { "else", new BsonArray() } // Si $dailyAccesses es null, inicializarlo como un array vacío
            })
        }
    })
});

            var query = finalUpdate.ToString();
            return new UpdateOneModel<UrlAnalytics>(
                Builders<UrlAnalytics>.Filter.Eq(x => x.ShortCode, shortCode),
                finalUpdate)
            {
                IsUpsert = true
            };
        }

        private async Task IncrementTotalCountAsync(string shortCode)
        {
            var update = Builders<UrlAnalytics>.Update
                .Inc(x => x.TotalAccessCount, 1);

            await _collection.UpdateOneAsync(
                x => x.ShortCode == shortCode,
                update,
                new UpdateOptions { IsUpsert = true });
        }

        public async Task<UrlAnalytics?> GetAnalyticsAsync(string shortCode)
        {
            return await _collection
                .Find(x => x.ShortCode == shortCode)
                .FirstOrDefaultAsync();
        }

        public async Task<Dictionary<DateTime, DailyStatsInfo>> GetDailyStatsAsync(
            string shortCode,
            DateTime startDate,
            DateTime endDate)
        {
            var analytics = await _collection
                .Find(x => x.ShortCode == shortCode)
                .FirstOrDefaultAsync();

            if (analytics == null)
                return new Dictionary<DateTime, DailyStatsInfo>();

            return analytics.DailyAccesses
                .Where(x => x.Date >= startDate && x.Date <= endDate)
                .ToDictionary(
                    x => x.Date,
                    x => new DailyStatsInfo
                    {
                        HourlyAccesses = Enumerable.Range(0, 24)
                            .ToDictionary(h => h, h => x.HourlyHits[h])
                    });
        }

        public async Task<Dictionary<int, int>?> GetHourlyStatsAsync(
            string shortCode,
            DateTime date)
        {
            var analytics = await _collection
                .Find(x => x.ShortCode == shortCode)
                .FirstOrDefaultAsync();

            var dailyAccess = analytics?.DailyAccesses
                .FirstOrDefault(x => x.Date.Date == date.Date);

            if (dailyAccess == null)
                return null;

            return Enumerable.Range(0, 24)
                .ToDictionary(h => h, h => dailyAccess.HourlyHits[h]);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _batchWriteTimer?.Dispose();
            _disposed = true;
        }

        private record AccessRecord(string ShortCode, DateTime AccessTime);
    }
}