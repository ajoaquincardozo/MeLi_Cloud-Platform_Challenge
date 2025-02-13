using Polly;

namespace MeLi.UrlShortener.Infrastructure.Resilience
{
    public static class ResiliencePolicies
    {
        public static IAsyncPolicy CreateAsyncPolicy()
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                .WrapAsync(CreateCircuitBreakerPolicy());
        }

        public static IAsyncPolicy<T> CreateAsyncPolicy<T>()
        {
            return Policy<T>
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                .WrapAsync(CreateCircuitBreakerPolicy<T>());
        }

        private static IAsyncPolicy CreateCircuitBreakerPolicy()
        {
            return Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30));
        }

        private static IAsyncPolicy<T> CreateCircuitBreakerPolicy<T>()
        {
            return Policy<T>
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30));
        }
    }
}