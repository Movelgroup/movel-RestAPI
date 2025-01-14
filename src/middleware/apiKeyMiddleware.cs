// ApiKeyMiddleware.cs
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using apiEndpointNameSpace.Services;


namespace apiEndpointNameSpace.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string APIKEYNAME = "X-Api-Key";
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private readonly IApiKeyProvider _apiKeyProvider;

        public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger, IApiKeyProvider apiKeyProvider)
        {
            _next = next;
            _logger = logger;
            _apiKeyProvider = apiKeyProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Check if API Key header is present
                if (!context.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
                {
                    _logger.LogWarning("API Key was not provided in the request headers.");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("API Key not provided.");
                    return;
                }

                // Fetch valid API keys from the provider
                var apiKeys = await _apiKeyProvider.GetApiKeysAsync();

                if (apiKeys == null || !apiKeys.Any())
                {
                    _logger.LogError("No valid API keys found in Secret Manager.");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Server configuration error.");
                    return;
                }

                // Check if the provided API key matches any valid key
                bool keyMatches = apiKeys.Any(key => string.Equals(key, extractedApiKey, StringComparison.Ordinal));
                if (!keyMatches)
                {
                    _logger.LogWarning("Unauthorized attempt with API Key: {ProvidedKey}", extractedApiKey);
#if DEBUG
                    // In development, provide a more explicit error message.
                    await context.Response.WriteAsync("Invalid API key provided.");
#else
                    await context.Response.WriteAsync("Unauthorized client.");
#endif
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                _logger.LogInformation("Authorized request with correct API Key.");
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing the API Key.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Internal server error.");
            }
        }
    }

}
