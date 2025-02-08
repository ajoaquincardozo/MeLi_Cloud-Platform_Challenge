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
            var updates = new List<UpdateDefinition<UrlAnalytics>>();

            foreach (var record in group)
            {
                var date = record.AccessTime.Date;
                var hour = record.AccessTime.Hour;

                updates.Add(Builders<UrlAnalytics>.Update
                    .Inc($"DailyAccesses.$[daily].HourlyHits[{hour}]", 1)
                    .Inc($"DailyAccesses.$[daily].TotalDayHits", 1)
                    .Set(x => x.LastCalculatedAt, DateTime.UtcNow));
            }

            var arrayFilters = new[]
            {
                new BsonDocumentArrayFilterDefinition<BsonDocument>(
                    new BsonDocument("daily.Date", group.First().AccessTime.Date))
            };

            return new UpdateOneModel<UrlAnalytics>(
                Builders<UrlAnalytics>.Filter.Eq(x => x.ShortCode, shortCode),
                Builders<UrlAnalytics>.Update.Combine(updates))
            {
                ArrayFilters = arrayFilters,
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