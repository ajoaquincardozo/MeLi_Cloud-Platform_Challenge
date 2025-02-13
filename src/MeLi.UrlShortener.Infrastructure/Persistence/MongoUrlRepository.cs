using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MeLi.UrlShortener.Domain.Interfaces;
using MeLi.UrlShortener.Infrastructure.Configuration;
using MeLi.UrlShortener.Domain.Entities;
using MeLi.UrlShortener.Infrastructure.Resilience;

namespace MeLi.UrlShortener.Infrastructure.Persistence
{
    public class MongoUrlRepository : IUrlRepository
    {
        private readonly IMongoCollection<UrlEntity> _urlCollection;

        public MongoUrlRepository(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _urlCollection = database.GetCollection<UrlEntity>(settings.Value.UrlsCollectionName);
        }

        public async Task<UrlEntity> SaveAsync(UrlEntity url)
        {
            return await ResiliencePolicies
                .CreateAsyncPolicy<UrlEntity>()
                .ExecuteAsync(async () =>
                {
                    await _urlCollection.InsertOneAsync(url);
                    return url;
                });
        }

        public async Task<UrlEntity> GetByShortCodeAsync(string shortCode)
        {
            return await ResiliencePolicies
                .CreateAsyncPolicy<UrlEntity>()
                .ExecuteAsync(async () =>
                {
                    var filter = Builders<UrlEntity>.Filter.Eq("shortCode", shortCode)
                        & Builders<UrlEntity>.Filter.Eq(x => x.IsActive, true);

                    return await _urlCollection.Find(filter).FirstOrDefaultAsync();
                });
        }

        public async Task<bool> ExistsAsync(string shortCode)
        {
            return await ResiliencePolicies
                .CreateAsyncPolicy<bool>()
                .ExecuteAsync(async () =>
                {
                    var filter = Builders<UrlEntity>.Filter.Eq("shortCode", shortCode)
                        & Builders<UrlEntity>.Filter.Eq(x => x.IsActive, true);

                    return await _urlCollection.Find(filter).AnyAsync();
                });
        }

        public async Task<bool> DeleteAsync(string shortCode)
        {
            return await ResiliencePolicies
                .CreateAsyncPolicy<bool>()
                .ExecuteAsync(async () =>
                {
                    var filter = Builders<UrlEntity>.Filter.Eq("shortCode", shortCode)
                        & Builders<UrlEntity>.Filter.Eq(x => x.IsActive, true);

                    var update = Builders<UrlEntity>.Update.Set(x => x.IsActive, false);
                    var result = await _urlCollection.UpdateOneAsync(filter, update);
                    return result.ModifiedCount > 0;
                });
        }
    }
}