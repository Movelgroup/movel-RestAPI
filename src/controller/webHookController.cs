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
            // Retrieve webhook secret from configuration
            var webhookSecret = _configuration["Webhooks:EmablerPushApiSecret"];

            // Validate webhook secret
            if (string.IsNullOrEmpty(webhookSecret) || 
                string.IsNullOrEmpty(authHeader) || 
                !authHeader.Equals($"Bearer {webhookSecret}", StringComparison.Ordinal))
            {
                _logger.LogWarning("Unauthorized webhook access attempt");
                return Unauthorized(new { 
                    status = "Error", 
                    message = "Unauthorized webhook access" 
                });
            }

            var activityId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            
            try
            {
                // Log the received payload with a unique activity ID for traceability
                _logger.LogInformation(
                    "ActivityId: {ActivityId} - Received eMabler webhook payload: {Payload}", 
                    activityId, 
                    payload.ToString()
                );

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
                    return BadRequest(new { 
                        status = "Error", 
                        message = "Unrecognized webhook payload",
                        activityId 
                    });
                }

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
                    "ActivityId: {ActivityId} - Error processing webhook payload: {ErrorMessage}", 
                    activityId, 
                    ex.Message
                );
                return StatusCode(500, new { 
                    status = "Error", 
                    message = "Failed to process webhook",
                    activityId 
                });
            }
        }

        // Helper methods to identify message types
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

        // Processing methods for different message types
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