using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models;
using System.Net;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Api;

namespace apiEndpointNameSpace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PushAPIController : ControllerBase
    {
        private readonly IDataProcessor _dataProcessor;
        private readonly IFirestoreService _firestoreService;
        private readonly INotificationService _notificationService;
        private readonly IAuthorizationService _authorizationService;

        public PushAPIController(
            IDataProcessor dataProcessor,
            IFirestoreService firestoreService,
            INotificationService notificationService,
            IAuthorizationService authorizationService)
        {
            _dataProcessor = dataProcessor;
            _firestoreService = firestoreService;
            _notificationService = notificationService;
            _authorizationService = authorizationService;
        }

        [HttpPost("charger-state")]
        public async Task<IActionResult> ReceiveChargerState([FromBody] ChargerStateMessage message, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("ReciveChargerStates");

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid ModelState in ReceiveChargerState");
                return BadRequest(ModelState);
            }

            try
            {
                var processedData = await _dataProcessor.ProcessChargerStateAsync(message);
                logger.LogInformation("Received ChargerState data, ChargerID: {ChargerId}", processedData.ChargerId);

                var storeInfo = await _firestoreService.StoreChargerStateAsync(processedData);
                logger.LogInformation("FirestoreInfo: {StoreInfo}", storeInfo);

                await _notificationService.NotifyChargerStateChangeAsync(processedData);
                
                logger.LogInformation("Charger state processed successfully for ChargerID: {ChargerId}", processedData.ChargerId);
                return Ok(new { Status = "Success", Message = "Charger state received and processed", DebugInfo = storeInfo });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing charger state");
                var errorRespose = new ErrorResponse
                {
                    Status = "Error",
                    Message = "An error occurred while processing the charger state",
                    ExceptionMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                };
                return StatusCode(500, errorRespose);
            }
        }

        [HttpPost("measurements")]
        public async Task<IActionResult> ReceiveMeasurements([FromBody] MeasurementsMessage message, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("ReciveChargerStates");

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid ModelState in ReceiveMeasurements");
                return BadRequest(ModelState);
            }

            try
            {
                var processedData = await _dataProcessor.ProcessMeasurementsAsync(message);
                await _firestoreService.StoreMeasurementsAsync(processedData);
                await _notificationService.NotifyMeasurementsUpdateAsync(processedData);

                logger.LogInformation("Measurements processed successfully for ChargerID: {ChargerId}", processedData.ChargerId);
                return Ok(new { Status = "Success", Message = "Measurements received and processed" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing measurements");
                return StatusCode(500, new { Status = "Error", Message = "An error occurred while processing the measurements" });
            }
        }

        [HttpGet("charger/{chargerId}")]
        public async Task<IActionResult> GetChargerData(string chargerId, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("ReciveChargerStates");

            try
            {
                var user = HttpContext.User;
                if (!await _authorizationService.CanAccessChargerDataAsync(user, chargerId))
                {
                    logger.LogWarning("Unauthorized access attempt for ChargerID: {ChargerId}", chargerId);
                    return Forbid();
                }

                var chargerData = await _firestoreService.GetChargerDataAsync(chargerId);
                logger.LogInformation("Charger data retrieved successfully for ChargerID: {ChargerId}", chargerId);
                return Ok(chargerData);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving charger data for ChargerID: {ChargerId}", chargerId);
                return StatusCode(500, new { Status = "Error", Message = "An error occurred while retrieving charger data" });
            }
        }
    }
}