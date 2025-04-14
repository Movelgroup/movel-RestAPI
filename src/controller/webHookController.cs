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

namespace apiEndpointNameSpace.Controllers.webhook
{
    /// <summary>
    /// Controller for handling webhooks from eMabler's Push API
    /// </summary>
    [ApiController]
    [Route("api/webHook")]
    [AllowAnonymous]
    public class EmablerWebhookController : ControllerBase
    {
        private readonly ILogger<EmablerWebhookController> _logger;
        private readonly IDataProcessor _dataProcessor;
        private readonly IFirestoreService _firestoreService;
        private readonly IConfiguration _configuration;

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
            IConfiguration configuration)
        {
            _logger = logger;
            _dataProcessor = dataProcessor;
            _firestoreService = firestoreService;
            _configuration = configuration;
        }

        /// <summary>
        /// Receives and processes webhook data from eMabler
        /// </summary>
        /// <param name="authHeader">Authorization header</param>
        /// <param name="payload">JSON payload from webhook</param>
        /// <returns>HTTP response indicating processing result</returns>
        [HttpPost]
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
                var webhookSecret = await GetWebhookSecretAsync();

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

                // Validate authorization header
                if (string.IsNullOrEmpty(authHeader))
                {
                    _logger.LogWarning("No authorization header provided");
                    return Unauthorized(new { 
                        status = "Error", 
                        message = "No authorization header" 
                    });
                }

                // Use case-insensitive comparison and trim the header
                var normalizedAuthHeader = authHeader.Trim();
                var expectedAuthHeader = $"Bearer {webhookSecret}";

                if (!normalizedAuthHeader.Equals(expectedAuthHeader, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Authorization header does not match expected value");
                    return Unauthorized(new { 
                        status = "Error", 
                        message = "Invalid authorization" 
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

        /// <summary>
        /// Retrieves the webhook secret with caching
        /// </summary>
        /// <returns>Webhook secret</returns>
        private async Task<string> GetWebhookSecretAsync()
        {
            // Check cache first
            if (_cachedWebhookSecret != null && 
                DateTime.UtcNow - _lastFetchTime < _cacheDuration)
            {
                _logger.LogInformation("Returning cached webhook secret");
                return _cachedWebhookSecret;
            }

            try 
            {
                var secretClient = SecretManagerServiceClient.Create();
                
                // Get project ID and secret ID from configuration
                string projectId = _configuration["GoogleCloudProjectId"];
                string secretId = _configuration["GoogleCloudSecrets:WebhookSecretId"];

                if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(secretId))
                {
                    _logger.LogError("Project ID or Webhook Secret ID is not configured");
                    return null;
                }

                var secretVersionName = new SecretVersionName(projectId, secretId, "latest");
                var request = new AccessSecretVersionRequest
                {
                    SecretVersionName = secretVersionName
                };

                var response = await secretClient.AccessSecretVersionAsync(request);
                string webhookSecret = response.Payload.Data.ToStringUtf8().Trim();

                // Cache the secret
                _cachedWebhookSecret = webhookSecret;
                _lastFetchTime = DateTime.UtcNow;

                _logger.LogInformation("Successfully retrieved and cached webhook secret from Secret Manager");
                return webhookSecret;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve webhook secret from Secret Manager");
                return null;
            }
        }

        /// <summary>
        /// Processes the webhook payload based on its type
        /// </summary>
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