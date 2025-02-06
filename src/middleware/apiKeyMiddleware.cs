// ApiKeyMiddleware.cs
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using apiEndpointNameSpace.Services;


namespace apiEndpointNameSpace.Middleware
{
    public class ApiKeyMiddleware(RequestDelegate _next, ILogger<ApiKeyMiddleware> _logger, IApiKeyProvider _apiKeyProvider)
    {
    private const string APIKEYNAME = "X-Api-Key";
    private const string CLIENTIDNAME = "X-Client-ID"; 
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var httpMethod = context.Request.Method;

            _logger.LogInformation("Incoming request: Path = {Path}, IP = {IP}, Method = {Method}", path, ipAddress, httpMethod);

            if (path.StartsWith("/swagger") || path.StartsWith("/swagger.json") || path.StartsWith("/health"))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(APIKEYNAME, out var extractedApiKey) ||
                !context.Request.Headers.TryGetValue(CLIENTIDNAME, out var extractedClientId))
            {
                _logger.LogWarning("Missing API Key or Client ID.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API Key or Client ID not provided.");
                return;
            }

            var apiKeys = await _apiKeyProvider.GetApiKeysAsync();

            var validEntry = apiKeys.FirstOrDefault(entry =>
                string.Equals(entry.ApiKey, extractedApiKey, StringComparison.Ordinal) &&
                string.Equals(entry.ClientId, extractedClientId, StringComparison.Ordinal));

            if (validEntry == null)
            {
                _logger.LogWarning("Unauthorized attempt with API Key: {ApiKey}, Client ID: {ClientId}", extractedApiKey, extractedClientId);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized client.");
                return;
            }

            _logger.LogInformation("Authorized request. Client ID: {ClientId}, API Key: {ApiKey}", extractedClientId, extractedApiKey);
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