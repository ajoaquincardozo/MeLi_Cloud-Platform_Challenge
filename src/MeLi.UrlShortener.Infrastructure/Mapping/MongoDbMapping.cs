using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MeLi.UrlShortener.Domain.Entities;

namespace MeLi.UrlShortener.Infrastructure.Persistence.Mapping
{
    public static class MongoDbMapping
    {
        public static void Configure()
        {
            // Convenciones globales
            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreIfNullConvention(true)
            };
            ConventionRegistry.Register("CustomConventions", pack, _ => true);

            // Configuración de mapeo para UrlEntity
            if (!BsonClassMap.IsClassMapRegistered(typeof(UrlEntity)))
            {
                BsonClassMap.RegisterClassMap<UrlEntity>(cm =>
                {
                    cm.SetIgnoreExtraElements(true);

                    // Mapeo del Id
                    cm.MapIdMember(c => c.Id)
                        .SetSerializer(new StringSerializer(BsonType.ObjectId));

                    // Mapeo de campos privados
                    cm.MapField("_longUrlString")
                        .SetElementName("longUrl");

                    cm.MapField("_shortCodeString")
                        .SetElementName("shortCode");

                    cm.MapField("_expirationDateValue")
                        .SetElementName("expiresAt");

                    // Mapeo de propiedades públicas
                    cm.MapProperty(c => c.CreatedAt)
                        .SetElementName("createdAt");

                    cm.MapProperty(c => c.LastAccessedAt)
                        .SetElementName("lastAccessedAt");

                    cm.MapProperty(c => c.AccessCount)
                        .SetElementName("accessCount");

                    cm.MapProperty(c => c.IsActive)
                        .SetElementName("isActive");

                    cm.MapProperty(c => c.CreatedBy)
                        .SetElementName("createdBy");

                    // Ignorar propiedades computadas
                    cm.UnmapProperty(c => c.LongUrl);
                    cm.UnmapProperty(c => c.ShortCode);
                    cm.UnmapProperty(c => c.ExpiresAt);
                });
            }
        }
    }
}