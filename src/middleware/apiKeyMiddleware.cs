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
                var path = context.Request.Path.Value;
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                var httpMethod = context.Request.Method;

                // Log request details
                _logger.LogInformation("Incoming request: Path = {Path}, IP = {IP}, Method = {Method}", path, ipAddress, httpMethod);

                // Bypass API key check for Swagger and other public endpoints
                if (path.StartsWith("/swagger") || path.StartsWith("/swagger.json") || path.StartsWith("/health"))
                {
                    await _next(context);
                    return;
                }

                // Check if API Key header is present
                if (!context.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
                {
                    _logger.LogWarning("API Key was not provided. Path: {Path}, IP: {IP}, Method: {Method}", path, ipAddress, httpMethod);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("API Key not provided.");
                    return;
                }

                // Fetch valid API keys from the provider
                var apiKeys = await _apiKeyProvider.GetApiKeysAsync();

                if (apiKeys == null || !apiKeys.Any())
                {
                    _logger.LogError("No valid API keys found in configuration. Path: {Path}, IP: {IP}, Method: {Method}", path, ipAddress, httpMethod);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Server configuration error.");
                    return;
                }

                // Validate the provided API key
                bool keyMatches = apiKeys.Any(key => string.Equals(key, extractedApiKey, StringComparison.Ordinal));
                if (!keyMatches)
                {
                    _logger.LogWarning("Unauthorized attempt with API Key: {ProvidedKey}, Path: {Path}, IP: {IP}, Method: {Method}",
                        extractedApiKey, path, ipAddress, httpMethod);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized client.");
                    return;
                }

                _logger.LogInformation("Authorized request. Path: {Path}, IP: {IP}, Method: {Method}, API Key: {ProvidedKey}",
                    path, ipAddress, httpMethod, extractedApiKey);
                await _next(context);
            }
            catch (Exception ex)
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                var path = context.Request.Path.Value;
                _logger.LogError(ex, "An error occurred. Path: {Path}, IP: {IP}", path, ipAddress);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Internal server error.");
            }
        }

    }

}
