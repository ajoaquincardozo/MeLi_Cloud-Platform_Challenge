using System.Net;
using System.Text.Json;

namespace MeLi.UrlShortener.Api.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ErrorHandlingMiddleware(
            RequestDelegate next,
            ILogger<ErrorHandlingMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

            var response = context.Response;
            response.ContentType = "application/json";

            var (statusCode, message) = GetErrorDetails(exception);
            response.StatusCode = (int)statusCode;

            var result = JsonSerializer.Serialize(new
            {
                statusCode,
                message,
                details = _env.IsDevelopment() ? exception.StackTrace : null
            });

            await response.WriteAsync(result);
        }

        private (HttpStatusCode statusCode, string message) GetErrorDetails(Exception exception)
        {
            return exception switch
            {
                ArgumentException _ => (HttpStatusCode.BadRequest, exception.Message),
                KeyNotFoundException _ => (HttpStatusCode.NotFound, "Resource not found"),
                InvalidOperationException _ => (HttpStatusCode.BadRequest, exception.Message),
                UnauthorizedAccessException _ => (HttpStatusCode.Unauthorized, "Unauthorized access"),
                _ => (HttpStatusCode.InternalServerError, "An internal error occurred. Please try again later.")
            };
        }
    }

    // Extension method para registrar el middleware
    public static class ErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandling(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}