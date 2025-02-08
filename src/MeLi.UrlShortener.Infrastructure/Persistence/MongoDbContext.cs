using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MeLi.UrlShortener.Infrastructure.Persistence.Mapping;
using MeLi.UrlShortener.Infrastructure.Configuration;
using MeLi.UrlShortener.Domain.Interfaces;
using MeLi.UrlShortener.Domain.Entities;

namespace MeLi.UrlShortener.Infrastructure.Persistence
{
    public interface IMongoDbContext
    {
        IMongoCollection<UrlEntity> Urls { get; }

        IMongoCollection<UrlAnalytics> UrlAnalytics { get; }

        void CreateIndexes();
    }

    public class MongoDbContext : IMongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly MongoDbSettings _config;

        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            MongoDbMapping.Configure();

            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
            _config = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        public IMongoCollection<UrlEntity> Urls =>
            _database.GetCollection<UrlEntity>(_config.UrlsCollectionName);

        public IMongoCollection<UrlAnalytics> UrlAnalytics =>
            _database.GetCollection<UrlAnalytics>(_config.UrlAnalyticsCollectionName);

        public void CreateIndexes()
        {
            CreateUrlsIndexes();
            CreateUrlAnalyticsIndexes();
        }

        private void CreateUrlsIndexes()
        {
            var indexKeysDefinition = Builders<UrlEntity>.IndexKeys
                .Ascending("shortCode");

            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<UrlEntity>(indexKeysDefinition, indexOptions);

            Urls.Indexes.CreateOne(indexModel);

            // Índice compuesto para búsquedas frecuentes
            var searchIndex = Builders<UrlEntity>.IndexKeys
                .Ascending(x => x.IsActive);

            Urls.Indexes.CreateOne(new CreateIndexModel<UrlEntity>(searchIndex));
        }

        private void CreateUrlAnalyticsIndexes()
        {
            var indexes = new[]
            {
                new CreateIndexModel<UrlAnalytics>(
                    Builders<UrlAnalytics>.IndexKeys
                        .Ascending(x => x.ShortCode)
                        .Ascending("DailyAccesses.Date")),

                new CreateIndexModel<UrlAnalytics>(
                    Builders<UrlAnalytics>.IndexKeys
                        .Ascending(x => x.ShortCode)
                        .Descending(x => x.TotalAccessCount))
            };

            UrlAnalytics.Indexes.CreateManyAsync(indexes);
        }
    }
}