namespace MeLi.UrlShortener.Infrastructure.Configuration
{
    public class MongoDbSettings
    {
        public const string SectionName = "MongoDB";

        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string CollectionName { get; set; } = string.Empty;

        public void Validate()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentNullException(nameof(ConnectionString), "MongoDB connection string is not configured");

            if (string.IsNullOrEmpty(DatabaseName))
                throw new ArgumentNullException(nameof(DatabaseName), "MongoDB database name is not configured");
        }
    }
}
