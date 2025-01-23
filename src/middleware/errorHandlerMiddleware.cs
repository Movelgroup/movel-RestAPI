using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace apiEndpointNameSpace.Middleware
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger, IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
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
            var traceId = Guid.NewGuid().ToString(); // Unique identifier for tracing
            var clientInfo = context.Request.Headers["X-Client-ID"].FirstOrDefault() ?? "Unknown";

            // Categorize the exception
            var (statusCode, message, details) = CategorizeException(exception);

            // Log the error with metadata
            _logger.LogError(exception, "Error occurred. TraceId: {TraceId}, Client: {ClientInfo}", traceId, clientInfo);

            // Build error response
            var response = new ErrorResponse
            {
                StatusCode = statusCode,
                Message = message,
                Details = _environment.IsDevelopment() ? details : null,
                TraceId = traceId
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsJsonAsync(response);
        }

        private (int statusCode, string message, string details) CategorizeException(Exception exception)
        {
            return exception switch
            {
                ValidationException => ((int)HttpStatusCode.BadRequest, "Validation failed.", exception.Message),
                AuthenticationException => ((int)HttpStatusCode.Unauthorized, "Authentication error.", exception.Message),
                DatabaseException => ((int)HttpStatusCode.InternalServerError, "Database error.", exception.Message),
                _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.", exception.Message)
            };
        }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string? Details { get; set; }
        public string TraceId { get; set; }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    public class AuthenticationException : Exception
    {
        public AuthenticationException(string message) : base(message) { }
    }

    public class DatabaseException : Exception
    {
        public DatabaseException(string message) : base(message) { }
    }
}
