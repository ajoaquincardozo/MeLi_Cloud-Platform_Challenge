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
            //_ = IncrementTotalCountAsync(shortCode);
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

                if (!batch.Any()) return;

                // Procesamos por shortCode
                foreach (var group in batch.GroupBy(x => x.ShortCode))
                {
                    var shortCode = group.Key;
                    await ProcessGroupAsync(shortCode, group);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw
            }
        }

        private async Task ProcessGroupAsync(string shortCode, IGrouping<string, AccessRecord> group)
        {
            // Agrupamos hits por hora
            var hitsByHour = group.GroupBy(x => x.AccessTime.Hour)
                                 .ToDictionary(x => x.Key, x => x.Count());

            var date = group.First().AccessTime.Date;

            // Buscamos el documento
            var analytics = await _collection.Find(x => x.ShortCode == shortCode).FirstOrDefaultAsync();

            if (analytics == null)
            {
                // Crear nuevo documento
                analytics = new UrlAnalytics
                (
                    shortCode: shortCode,
                    dailyAccesses: new List<DailyAccess>
                    {
                        CreateNewDailyAccess(date, hitsByHour)
                    },
                    lastCalculatedAt: DateTime.UtcNow,
                    totalAccessCount: group.Count()
                );
                await _collection.InsertOneAsync(analytics);
                return;
            }

            // Actualizar documento existente
            var dailyAccess = analytics.DailyAccesses.FirstOrDefault(x => x.Date.Date == date);

            var update = dailyAccess == null
                ? Builders<UrlAnalytics>.Update
                    .Push(x => x.DailyAccesses, CreateNewDailyAccess(date, hitsByHour))
                : CreateUpdateForExistingDay(date, hitsByHour);

            update = update.Set(x => x.LastCalculatedAt, DateTime.UtcNow)
                          .Inc(x => x.TotalAccessCount, group.Count());

            await _collection.UpdateOneAsync(x => x.Id == analytics.Id, update);
        }

        private DailyAccess CreateNewDailyAccess(DateTime date, Dictionary<int, int> hitsByHour)
        {
            var hourlyHits = new int[24];
            var totalHits = 0;

            foreach (var (hour, hits) in hitsByHour)
            {
                hourlyHits[hour] = hits;
                totalHits += hits;
            }

            return new DailyAccess
            {
                Date = date,
                HourlyHits = hourlyHits,
                TotalDayHits = totalHits
            };
        }

        private UpdateDefinition<UrlAnalytics> CreateUpdateForExistingDay(DateTime date, Dictionary<int, int> hitsByHour)
        {
            var update = Builders<UrlAnalytics>.Update;
            var updates = new List<UpdateDefinition<UrlAnalytics>>();

            foreach (var (hour, hits) in hitsByHour)
            {
                updates.Add(update.Inc($"DailyAccesses.$[day].HourlyHits.{hour}", hits));
            }
            updates.Add(update.Inc($"DailyAccesses.$[day].TotalDayHits", hitsByHour.Values.Sum()));

            return update.Combine(updates);
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