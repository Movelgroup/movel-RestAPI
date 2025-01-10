// ApiKeyMiddleware.cs
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;


namespace apiEndpointNameSpace.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string APIKEYNAME = "X-Api-Key";
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
            {
                _logger.LogWarning("API Key was not provided.");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key was not provided.");
                return;
            }

            var apiKeys = _configuration.GetSection("ApiKeys:ValidKeys").GetChildren()
                .Select(k => k.Value).ToArray();
            if (apiKeys == null || !apiKeys.Any(key => string.Equals(key, extractedApiKey, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Unauthorized client attempted to access with invalid API Key.");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized client.");
                return;
            }

            await _next(context);
        }
        
    }
    // Extension Method for Middleware
    public static class ApiKeyMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyMiddleware>();
        }
    }
}