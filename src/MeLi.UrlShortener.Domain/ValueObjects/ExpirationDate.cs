namespace MeLi.UrlShortener.Domain.ValueObjects
{
    public class ExpirationDate
    {
        public DateTime? Value { get; }
        private const int MaxDaysInFuture = 365;

        private ExpirationDate(DateTime? value)
        {
            Value = value;
        }

        public static ExpirationDate Create(DateTime? date)
        {
            if (!date.HasValue)
                return new ExpirationDate(null);

            if (date.Value <= DateTime.UtcNow)
                throw new ArgumentException("Expiration date must be in the future", nameof(date));

            if (date.Value > DateTime.UtcNow.AddDays(MaxDaysInFuture))
                throw new ArgumentException($"Expiration date cannot be more than {MaxDaysInFuture} days in the future", nameof(date));

            return new ExpirationDate(date.Value);
        }

        public static implicit operator DateTime?(ExpirationDate expirationDate) => expirationDate.Value;

        public bool IsExpired() => Value.HasValue && Value.Value <= DateTime.UtcNow;
    }
}