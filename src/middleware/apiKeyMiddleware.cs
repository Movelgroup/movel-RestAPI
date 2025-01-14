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
            // Check if API Key header is present
            if (!context.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
            {
                _logger.LogWarning("API Key was not provided in the request headers.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API Key not provided.");
                return;
            }

            string[] apiKeys = null;
            try
            {
                // Retrieve valid API keys from configuration
                apiKeys = _configuration.GetSection("ApiKeys:ValidKeys")
                                        .GetChildren()
                                        .Select(k => k.Value)
                                        .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API keys from configuration.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Server configuration error.");
                return;
            }

            // Validate that we have a list of keys loaded
            if (apiKeys == null || !apiKeys.Any())
            {
                _logger.LogWarning("No API keys configured in the system.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Server configuration error.");
                return;
            }

            // Check if the provided API key matches any valid key
            bool keyMatches = apiKeys.Any(key => string.Equals(key, extractedApiKey, StringComparison.OrdinalIgnoreCase));
            if (!keyMatches)
            {
                _logger.LogWarning(
                    "Unauthorized attempt with API Key: {ProvidedKey}. Valid keys count: {Count}",
                    extractedApiKey,
                    apiKeys.Length
                );

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
#if DEBUG
                // In development, provide a more explicit error message.
                await context.Response.WriteAsync("Invalid API key provided.");
#else
                await context.Response.WriteAsync("Unauthorized client.");
#endif
                return;
            }

            _logger.LogInformation("Authorized request with correct API Key.");
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