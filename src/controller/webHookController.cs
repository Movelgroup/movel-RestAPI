using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models.ChargerData;
using apiEndpointNameSpace.Models.Measurements;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Google.Cloud.SecretManager.V1;

using Microsoft.AspNetCore.Http; // Needed for StatusCodes
using Swashbuckle.AspNetCore.Annotations; // Needed for Swagger annotations
using Microsoft.Extensions.Primitives; // Needed for StringValues type hint

using Microsoft.AspNetCore.WebUtilities; // For Base64UrlDecode
using System.Text; // For Encoding.UTF8



namespace apiEndpointNameSpace.Controllers.webhook
{
    using apiEndpointNameSpace.Models.Responses;

    /// <summary>
    /// Controller for handling incoming webhooks from eMabler's Push API.
    /// </summary>
    /// <remarks>
    /// This endpoint receives various types of messages pushed by eMabler.
    /// Authentication is handled via a custom Authorization header containing a shared secret.
    /// </remarks>
    [ApiController]
    [Route("api/webHook")]
    [AllowAnonymous]
    [Tags("Webhooks")] // Groups this controller under "Webhooks" in Swagger UI
    public class EmablerWebhookController : ControllerBase
    {
        private readonly ILogger<EmablerWebhookController> _logger;
        private readonly IDataProcessor _dataProcessor;
        private readonly IFirestoreService _firestoreService;
        private readonly IConfiguration _configuration;
        private readonly IWebhookSecretProvider _webhookSecretProvider;

        // Caching fields for webhook secret
        private string _cachedWebhookSecret;
        private DateTime _lastFetchTime;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);

        /// <summary>
        /// Constructor for EmablerWebhookController
        /// </summary>
        public EmablerWebhookController(
            ILogger<EmablerWebhookController> logger,
            IDataProcessor dataProcessor,
            IFirestoreService firestoreService,
            IConfiguration configuration,
            IWebhookSecretProvider webhookSecretProvider)
        {
            _logger = logger;
            _dataProcessor = dataProcessor;
            _firestoreService = firestoreService;
            _configuration = configuration;
            _webhookSecretProvider = webhookSecretProvider;
        }

        /// <summary>
        /// Receives and processes webhook push data from eMabler.
        /// </summary>
        /// <param name="authHeader" example="your_shared_secret_value">
        /// **Required.** The raw shared secret provided during eMabler webhook configuration.
        /// **Important:** Do **NOT** include the "Bearer " prefix.
        /// </param>
        /// <param name="payload">
        /// The JSON payload from the webhook. The structure varies depending on the message type pushed by eMabler.
        /// See Remarks section for examples.
        /// </param>
        /// <remarks>
        /// This endpoint handles various message types from eMabler. Authentication requires the exact shared secret configured in eMabler to be passed in the `Authorization` header.
        ///
        /// **Example Payload Structures:**
        ///
        /// *Charger State:*
        /// ```json
        /// {
        ///   "chargerId": "CHG-123",
        ///   "status": "AVAILABLE",
        ///   "errorCode": "NONE",
        ///   "timestamp": "2024-04-22T10:30:00Z"
        /// }
        /// ```
        ///
        /// *Measurements:*
        /// ```json
        /// {
        ///   "chargerId": "CHG-123",
        ///   "connectorId": 1,
        ///   "transactionId": "TXN-456",
        ///   "measurements": [
        ///     { "type": "ENERGY_ACTIVE_IMPORT_REGISTER", "value": 15.5, "unit": "kWh", "timestamp": "2024-04-22T10:35:00Z" },
        ///     { "type": "POWER_ACTIVE_IMPORT", "value": 7.2, "unit": "kW", "timestamp": "2024-04-22T10:35:00Z" }
        ///   ]
        /// }
        /// ```
        ///
        /// *Full Charging Transaction:*
        /// ```json
        /// {
        ///   "transactionId": "TXN-789",
        ///   "chargerId": "CHG-456",
        ///   "connectorId": 1,
        ///   "idTag": "RFID123",
        ///   "meterStart": 100.0,
        ///   "meterStop": 125.5,
        ///   "startTime": "2024-04-22T09:00:00Z",
        ///   "stopTime": "2024-04-22T11:00:00Z",
        ///   "deviceTimeStampStart": "2024-04-22T09:00:05Z",
        ///   "deviceTimeStampEnd": "2024-04-22T11:00:10Z",
        ///   "stopReason": "REMOTE"
        /// }
        /// ```
        ///
        /// *Charging Transaction Start/Stop Action:*
        /// ```json
        /// {
        ///   "transactionId": "TXN-999",
        ///   "chargerId": "CHG-789",
        ///   "action": "START", // or "STOP"
        ///   "idTag": "RFID456",
        ///   "timestamp": "2024-04-22T12:00:00Z"
        /// }
        /// ```
        /// </remarks>
        /// <returns>HTTP response indicating processing result (OK, Unauthorized, or InternalServerError).</returns>
        [HttpPost]
        [Consumes("application/json")] // Explicitly state expected request content type
        [Produces("application/json")] // Explicitly state response content type
        [SwaggerOperation(
            Summary = "Receives webhook data from eMabler",
            Description = "Processes various push messages from eMabler after validating the custom Authorization header.",
            OperationId = "ReceiveEmablerWebhook" // Unique ID for the operation
        )]
        [ProducesResponseType(typeof(WebhookSuccessResponse), StatusCodes.Status200OK)] // Success
        [ProducesResponseType(typeof(WebhookErrorResponse), StatusCodes.Status401Unauthorized)] // Auth failed
        [ProducesResponseType(typeof(WebhookErrorResponse), StatusCodes.Status500InternalServerError)] // Server error (config or processing)
        public async Task<IActionResult> ReceiveWebhookData(
        [FromHeader(Name = "Authorization")] string authHeader, 
        [FromBody] JsonElement payload)
        {
            try 
            {
                // Log all incoming headers for debugging
                foreach (var header in Request.Headers)
                {
                    _logger.LogInformation($"Header - {header.Key}: {header.Value}");
                }

                // Retrieve webhook secret with caching
                var webhookSecret = _webhookSecretProvider.GetSecret();

                // Log sensitive information carefully
                _logger.LogInformation("Webhook secret retrieval attempt completed");
                _logger.LogInformation($"Received Authorization Header: {(authHeader != null ? "[REDACTED]" : "NULL")}");

                // Validate webhook secret
                if (string.IsNullOrEmpty(webhookSecret))
                {
                    _logger.LogError("Webhook secret is not configured in Secret Manager");
                    return StatusCode(500, new { 
                        status = "Error", 
                        message = "Webhook configuration error" 
                    });
                }

                // Validate authorization header format
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.Contains(' '))
                {
                    _logger.LogWarning("Invalid Authorization header format");
                    return Unauthorized(new {
                        status = "Error",
                        message = "Invalid authorization format. Expected '<scheme> <token>'"
                    });
                }

                var parts = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var scheme = parts[0];
                var token = parts[1];

                if (!scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Unsupported Authorization scheme: {Scheme}", scheme);
                    return Unauthorized(new {
                        status = "Error",
                        message = "Unsupported authorization scheme"
                    });
                }

                // Compare token to the secret
                if (token != webhookSecret)
                {
                    _logger.LogWarning("Authorization token does not match expected secret");
                    return Unauthorized(new {
                        status = "Error",
                        message = "Invalid authorization token"
                    });
                }


                // Generate an activity ID for tracing
                var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
                
                // Log the received payload
                _logger.LogInformation(
                    "ActivityId: {ActivityId} - Received eMabler webhook payload: {Payload}", 
                    activityId, 
                    payload.ToString()
                );

                // Process message based on its type
                await ProcessWebhookPayload(payload, activityId);

                return Ok(new { 
                    status = "Success", 
                    message = "Webhook data processed successfully",
                    activityId 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, 
                    "Error processing webhook: {ErrorMessage}", 
                    ex.Message
                );
                return StatusCode(500, new { 
                    status = "Error", 
                    message = "Failed to process webhook"
                });
            }
        }


        [HttpGet]
        [SwaggerOperation(
            Summary = "Validation endpoint for webhook Authorization header",
            Description = "Responds with HTTP 200 OK if Authorization header is valid",
            OperationId = "ValidateWebhookAuth"
        )]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ValidateWebhookAuth(
            [FromHeader(Name = "Authorization")] string authHeader)
        {
            var webhookSecret = _webhookSecretProvider.GetSecret();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.Contains(' '))
            {
                return Unauthorized();
            }

            var parts = authHeader.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized();
            }

            var token = parts[1];
            if (token != webhookSecret)
            {
                return Unauthorized();
            }

            return Ok(); // eMabler will expect 200 OK if valid
        }



        /// <summary>
        /// Determines the type of the incoming webhook payload and routes it to the appropriate processing method.
        /// </summary>
        /// <param name="payload">The raw JSON payload.</param>
        /// <param name="activityId">The trace activity ID.</param>
        /// <exception cref="ArgumentException">Thrown if the payload structure is not recognized.</exception>
        private async Task ProcessWebhookPayload(JsonElement payload, string activityId)
        {
            try 
            {
                // Determine and process message type
                if (IsChargerStateMessage(payload))
                {
                    await ProcessChargerStateMessage(payload);
                }
                else if (IsMeasurementsMessage(payload))
                {
                    await ProcessMeasurementsMessage(payload);
                }
                else if (IsFullChargingTransactionMessage(payload))
                {
                    await ProcessFullChargingTransactionMessage(payload);
                }
                else if (IsChargingTransactionMessage(payload))
                {
                    await ProcessChargingTransactionMessage(payload);
                }
                else
                {
                    _logger.LogWarning(
                        "ActivityId: {ActivityId} - Unrecognized webhook payload structure", 
                        activityId
                    );
                    throw new ArgumentException("Unrecognized webhook payload structure");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, 
                    "ActivityId: {ActivityId} - Error processing payload: {ErrorMessage}", 
                    activityId, 
                    ex.Message
                );
                throw; // Rethrow to be handled by the calling method
            }
        }

        // Existing helper methods for message type detection...
        private bool IsChargerStateMessage(JsonElement payload) =>
            payload.TryGetProperty("status", out _) && 
            payload.TryGetProperty("chargerId", out _);

        private bool IsMeasurementsMessage(JsonElement payload) =>
            payload.TryGetProperty("measurements", out _) && 
            payload.TryGetProperty("chargerId", out _);

        private bool IsFullChargingTransactionMessage(JsonElement payload) =>
            payload.TryGetProperty("transactionId", out _) && 
            payload.TryGetProperty("deviceTimeStampStart", out _) && 
            payload.TryGetProperty("deviceTimeStampEnd", out _);

        private bool IsChargingTransactionMessage(JsonElement payload) =>
            payload.TryGetProperty("transactionId", out _) && 
            payload.TryGetProperty("action", out _);

        // Existing processing methods for different message types...
        private async Task ProcessChargerStateMessage(JsonElement payload)
        {
            var chargerState = payload.Deserialize<ChargerStateMessage>();
            var processedState = await _dataProcessor.ProcessChargerStateAsync(chargerState);
            await _firestoreService.StoreChargerStateAsync(processedState);
        }

        private async Task ProcessMeasurementsMessage(JsonElement payload)
        {
            var measurements = payload.Deserialize<MeasurementsMessage>();
            var processedMeasurements = await _dataProcessor.ProcessMeasurementsAsync(measurements);
            await _firestoreService.StoreMeasurementsAsync(processedMeasurements);
        }

        private async Task ProcessFullChargingTransactionMessage(JsonElement payload)
        {
            var fullTransaction = payload.Deserialize<FullChargingTransaction>();
            await _dataProcessor.ProcessFullChargingTransactionAsync(fullTransaction);
        }

        private async Task ProcessChargingTransactionMessage(JsonElement payload)
        {
            var chargingTransaction = payload.Deserialize<ChargingTransaction>();
            await _dataProcessor.ProcessChargingTransactionAsync(chargingTransaction);
        }
    }
}