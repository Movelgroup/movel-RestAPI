// ApiKeyMiddleware.cs
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using apiEndpointNameSpace.Services;


namespace apiEndpointNameSpace.Middleware
{
    public class ApiKeyMiddleware(
    RequestDelegate _next, 
    ILogger<ApiKeyMiddleware> _logger, 
    IApiKeyProvider _apiKeyProvider)
    {
        private const string APIKEYNAME = "X-Api-Key";

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                var path = context.Request.Path.Value ?? string.Empty;
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                var httpMethod = context.Request.Method;
                
                _logger.LogInformation("Incoming request: Path = {Path}, IP = {IP}, Method = {Method}", 
                    path, ipAddress, httpMethod);

                // Skip authentication for swagger, health check
                if (path.StartsWith("/swagger") || 
                    path.StartsWith("/swagger.json") || 
                    path.StartsWith("/health"))
                    path.StartsWith("/api/webHook"); // Skip webhook as it has its own method for auth 
                {
                    await _next(context);
                    return;
                }

                // Check if API key is present
                if (!context.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey))
                {
                    _logger.LogWarning("Missing API Key.");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("API Key not provided.");
                    return;
                }

                var apiKeys = await _apiKeyProvider.GetApiKeysAsync();
                var validEntry = apiKeys.FirstOrDefault(entry => 
                    string.Equals(entry.ApiKey, extractedApiKey, StringComparison.Ordinal));

                if (validEntry == null)
                {
                    _logger.LogWarning("Unauthorized attempt with API Key: {ApiKey}", extractedApiKey);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized client.");
                    return;
                }

                _logger.LogInformation("Authorized request. API Key validated.");
                await _next(context);
            }
            catch (Exception ex)
            {
                var path = context.Request.Path.Value;
                _logger.LogError(ex, "Error processing request. Path: {Path}", path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Internal server error.");
            }
        }
    }
}