using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MeLi.UrlShortener.Domain.Entities;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MeLi.UrlShortener.Infrastructure.Persistence.Mapping
{
    public static class MongoDbMapping
    {
        public static void Configure()
        {
            // Convenciones globales
            //BsonDefaults.GuidRepresentationMode = GuidRepresentationMode.V3;
            //BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            //BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
            //BsonSerializer.RegisterSerializer(new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));

            var pack = new ConventionPack
            {
                new IgnoreExtraElementsConvention(true),
                new CamelCaseElementNameConvention(),
                new IgnoreIfNullConvention(true)
            };

            ConventionRegistry.Register("CustomConventions", pack, _ => true);

            // Configuración de mapeo para UrlEntity
            if (!BsonClassMap.IsClassMapRegistered(typeof(UrlEntity)))
            {
                BsonClassMap.RegisterClassMap<UrlEntity>(cm =>
                {
                    // Mapeo del Id
                    cm.MapIdMember(c => c.Id)
                        .SetSerializer(new StringSerializer(BsonType.String))
                        .SetIdGenerator(StringObjectIdGenerator.Instance);

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