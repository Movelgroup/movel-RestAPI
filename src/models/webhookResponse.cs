
using Microsoft.AspNetCore.Http; // Needed for StatusCodes
using Swashbuckle.AspNetCore.Annotations; // Needed for Swagger annotations
using Microsoft.Extensions.Primitives; // Needed for StringValues type hint


// Define simple classes for Swagger response schemas (avoids anonymous types)
namespace apiEndpointNameSpace.Models.Responses // Choose an appropriate namespace
{
    public class WebhookSuccessResponse
    {
        /// <summary>Example: "Success"</summary>
        public string Status { get; set; } = "Success";
        /// <summary>Example: "Webhook data processed successfully"</summary>
        public string? Message { get; set; }
        /// <summary>Trace identifier for the request processing.</summary>
        public string? ActivityId { get; set; }
    }

    public class WebhookErrorResponse
    {
        /// <summary>Example: "Error"</summary>
        public string Status { get; set; } = "Error";
        /// <summary>Describes the error that occurred.</summary>
        public string? Message { get; set; }
    }
}