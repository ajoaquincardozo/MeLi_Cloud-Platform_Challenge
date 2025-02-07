namespace MeLi.UrlShortener.Infrastructure.Configuration
{
    public class RedisSettings
    {
        public const string SectionName = "Redis";
        public string ConnectionString { get; set; } = string.Empty;

        public void Validate()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                throw new ArgumentNullException(nameof(ConnectionString), "Redis connection string is not configured");
        }
    }
}