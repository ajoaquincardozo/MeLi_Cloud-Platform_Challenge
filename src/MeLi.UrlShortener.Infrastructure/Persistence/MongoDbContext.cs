using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MeLi.UrlShortener.Domain.Entities;
using MeLi.UrlShortener.Infrastructure.Persistence.Mapping;
using MeLi.UrlShortener.Infrastructure.Configuration;

namespace MeLi.UrlShortener.Infrastructure.Persistence
{
    public interface IMongoDbContext
    {
        IMongoCollection<UrlEntity> Urls { get; }

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
            _database.GetCollection<UrlEntity>(_config.CollectionName);

        public void CreateIndexes()
        {
            var indexKeysDefinition = Builders<UrlEntity>.IndexKeys
                .Ascending("shortCode");

            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<UrlEntity>(indexKeysDefinition, indexOptions);

            Urls.Indexes.CreateOne(indexModel);

            // Índice compuesto para búsquedas frecuentes
            var searchIndex = Builders<UrlEntity>.IndexKeys
                .Ascending(x => x.IsActive)
                .Descending(x => x.AccessCount);

            Urls.Indexes.CreateOne(new CreateIndexModel<UrlEntity>(searchIndex));
        }
    }
}