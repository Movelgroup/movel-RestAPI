// ApiKeyMiddleware.cs
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
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
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var httpMethod = context.Request.Method;

                _logger.LogInformation("Incoming request: Method = {Method}, Path = {Path}, IP = {IP}",
                    httpMethod, path, ipAddress);

                // --- Corrected Path Skipping Logic ---
                // Define paths that do not require API key validation. Use OrdinalIgnoreCase for robust path matching.
                bool skipValidation =
                    path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                    // Be specific about the swagger JSON file if needed, adjust path as necessary
                    path.Equals("/swagger/v1/swagger.json", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("/api/webHook", StringComparison.OrdinalIgnoreCase); // Skip webhook path

                if (skipValidation)
                {
                    _logger.LogInformation("Skipping API Key validation for path: {Path}", path);
                    await _next(context); // Pass control to the next middleware
                    return; // Exit this middleware
                }

                // --- API Key Validation Logic ---

                // 1. Check if the API Key header exists
                if (!context.Request.Headers.TryGetValue(APIKEYNAME, out StringValues extractedApiKeys) || extractedApiKeys.Count == 0)
                {
                    _logger.LogWarning("API Key header ('{ApiKeyHeaderName}') missing.", APIKEYNAME);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync($"API Key header '{APIKEYNAME}' is missing.");
                    return;
                }

                // 2. Check if the API Key header value is empty (headers can have multiple values)
                var extractedApiKey = extractedApiKeys.FirstOrDefault(); // Take the first value if multiple are sent
                if (string.IsNullOrWhiteSpace(extractedApiKey))
                {
                    _logger.LogWarning("API Key header ('{ApiKeyHeaderName}') value is empty or whitespace.", APIKEYNAME);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync($"API Key header '{APIKEYNAME}' value cannot be empty.");
                    return;
                }

                // 3. Get the valid keys from the provider (ensure this uses async correctly and caches results)
                var validApiKeys = await _apiKeyProvider.GetApiKeysAsync();
                if (validApiKeys == null)
                {
                    _logger.LogError("API Key provider returned null."); // Or handle as appropriate
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Error validating API Key configuration.");
                    return;
                }

                // 4. Validate the extracted key against the list (use Ordinal for case-sensitive comparison)
                // Replace 'YourApiKeyEntryType' with the actual type returned by GetApiKeysAsync()
                var validEntry = validApiKeys.FirstOrDefault(entry =>
                    entry != null && !string.IsNullOrEmpty(entry.ApiKey) &&
                    string.Equals(entry.ApiKey, extractedApiKey, StringComparison.Ordinal)); // Case-sensitive comparison

                if (validEntry == null)
                {
                    _logger.LogWarning("Unauthorized API Key provided: '{ProvidedApiKey}'", extractedApiKey);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized client: Invalid API Key.");
                    return;
                }

                // --- Success ---
                _logger.LogInformation("Authorized request via API Key."); // Optionally log validEntry.ClientId if it exists

                // Optional: Add validated information to HttpContext for downstream use if needed
                // context.Items["ApiClientId"] = validEntry.ClientId; // Example

                // Pass control to the next middleware in the pipeline
                await _next(context);
            }
            // --- Global Exception Handling for this Middleware ---
            catch (Exception ex)
            {
                // Log the exception specific to this middleware's failure
                _logger.LogError(ex, "Unhandled exception occurred in ApiKeyMiddleware.");

                // Avoid modifying the response if it has already started sending
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    // Avoid writing detailed errors to the client in production
                    await context.Response.WriteAsync("An internal server error occurred while processing the request.");
                }
                // Do not re-throw unless you have a specific reason; let the pipeline end here for this request.
                // If you have the ErrorHandlerMiddleware placed *before* this one, it won't catch this.
                // If ErrorHandlerMiddleware is placed *after* this one, re-throwing would trigger it.
                // Generally, handling it here and stopping might be sufficient.
            }
        }
    }
}