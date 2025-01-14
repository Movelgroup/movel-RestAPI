// ApiKeyMiddlewareExtensions.cs
using Microsoft.AspNetCore.Builder;

namespace apiEndpointNameSpace.Middleware
{
    public static class ApiKeyMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyMiddleware>();
        }
    }
}
