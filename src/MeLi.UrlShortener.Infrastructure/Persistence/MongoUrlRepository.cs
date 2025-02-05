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

            CreateIndexes();
        }

        private void CreateIndexes()
        {
            // Índice único para shortCode
            var shortCodeIndex = Builders<UrlEntity>.IndexKeys.Ascending(x => x.ShortCode);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<UrlEntity>(shortCodeIndex, indexOptions);
            _urlCollection.Indexes.CreateOne(indexModel);

            // Índice para búsquedas frecuentes
            var searchIndex = Builders<UrlEntity>.IndexKeys
                .Ascending(x => x.IsActive)
                .Descending(x => x.AccessCount);
            _urlCollection.Indexes.CreateOne(new CreateIndexModel<UrlEntity>(searchIndex));
        }

        public async Task<UrlEntity> SaveAsync(UrlEntity url)
        {
            await _urlCollection.InsertOneAsync(url);
            return url;
        }

        public async Task<UrlEntity?> GetByShortCodeAsync(string shortCode)
        {
            return await _urlCollection.Find(x => x.ShortCode == shortCode && x.IsActive)
                                     .FirstOrDefaultAsync();
        }

        public async Task<bool> ExistsAsync(string shortCode)
        {
            return await _urlCollection.Find(x => x.ShortCode == shortCode)
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

            await _urlCollection.UpdateOneAsync(
                x => x.ShortCode == shortCode && x.IsActive,
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