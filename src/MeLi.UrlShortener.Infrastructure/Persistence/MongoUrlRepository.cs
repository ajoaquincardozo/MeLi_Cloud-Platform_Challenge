using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MeLi.UrlShortener.Domain.Entities;
using MeLi.UrlShortener.Domain.Interfaces;
using MeLi.UrlShortener.Infrastructure.Configuration;

// Implementación actualizada del repositorio
namespace MeLi.UrlShortener.Infrastructure.Persistence
{
    public class MongoUrlRepository : IUrlRepository
    {
        private readonly IMongoCollection<UrlEntity> _urlCollection;

        public MongoUrlRepository(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _urlCollection = database.GetCollection<UrlEntity>(settings.Value.CollectionName);
        }

        public async Task<UrlEntity> SaveAsync(UrlEntity url)
        {
            await _urlCollection.InsertOneAsync(url);
            return url;
        }

        public async Task<UrlEntity?> GetByShortCodeAsync(string shortCode)
        {
            var filter = Builders<UrlEntity>.Filter.Eq("shortCode", shortCode);

            return await _urlCollection.Find(filter)
                                     .FirstOrDefaultAsync();
        }

        public async Task<bool> ExistsAsync(string shortCode)
        {
            var filter = Builders<UrlEntity>.Filter.Eq("shortCode", shortCode);

            return await _urlCollection.Find(filter)
                                     .AnyAsync();
        }

        public async Task<bool> DeleteAsync(string shortCode)
        {
            var update = Builders<UrlEntity>.Update.Set(x => x.IsActive, false);
            var result = await _urlCollection.UpdateOneAsync(
                x => x.ShortCode == shortCode && x.IsActive,
                update);

            return result.ModifiedCount > 0;
        }

        public async Task IncrementAccessCountAsync(string shortCode)
        {
            var update = Builders<UrlEntity>.Update
                .Inc(x => x.AccessCount, 1)
                .Set(x => x.LastAccessedAt, DateTime.UtcNow);

            var filterByShortCode = Builders<UrlEntity>.Filter.Eq("shortCode", shortCode);
            var filterIsActive = Builders<UrlEntity>.Filter.Eq(x => x.IsActive, true);

            await _urlCollection.UpdateOneAsync(
                filterByShortCode & filterIsActive,
                update);
        }

        public async Task<UrlStatistics?> GetStatisticsAsync(string shortCode)
        {
            var url = await _urlCollection
                .Find(x => x.ShortCode == shortCode)
                .Project(x => new UrlStatistics
                {
                    TotalAccesses = x.AccessCount,
                    LastAccessed = x.LastAccessedAt,
                    CreatedAt = x.CreatedAt,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();
            return url;
        }

        public async Task<(List<UrlEntity> Urls, int TotalCount)> GetMostAccessedAsync(int page, int pageSize)
        {
            var filter = Builders<UrlEntity>.Filter.Eq(x => x.IsActive, true);
            var totalCount = await _urlCollection.CountDocumentsAsync(filter);

            var urls = await _urlCollection
            .Find(filter)
                .Sort(Builders<UrlEntity>.Sort.Descending(x => x.AccessCount))
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (urls, (int)totalCount);
        }
    }
}