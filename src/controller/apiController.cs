using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models;
using System.Net;

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
        // ChargerState message
        /*
        This message is used to deliver the current status of a charging station and it’s socket Status values are: Available, Error, Offline, Info, Charging, SuspendedCAR, SuspendedCHARGER, Preparing, Finishing, Booting, Unavailable
        */
        public async Task<IActionResult> ReceiveChargerState([FromBody] ChargerStateMessage message, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("ReceiveChargerStates");

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
                var errorResponse = new ErrorResponse
                {
                    Status = "Error",
                    Message = "An error occurred while processing the charger state",
                    ExceptionMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                };
                return StatusCode(500, errorResponse);
            }
        }


        [HttpPost("measurements")]
        // Measurements message
        /*
        This message is send when charger is reporting meter values during the charging transaction. TypeofMeasurement, Phase and Unit are following OCPP1.6 model.
        */
        public async Task<IActionResult> ReceiveMeasurements([FromBody] MeasurementsMessage message, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("ReceiveMeasurements");

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

        // Endpoint for FullChargingTransaction
        /*
        This message is used to deliver information of a full charging transaction. This message is delivered after the charging has ended.
        */
        [HttpPost("full-charging-transaction")]
        public async Task<IActionResult> ReceiveFullChargingTransaction([FromBody] FullChargingTransaction message, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("ReceiveFullChargingTransaction");

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid ModelState in ReceiveFullChargingTransaction");
                return BadRequest(ModelState);
            }

            try
            {
                var processedData = await _dataProcessor.ProcessFullChargingTransactionAsync(message);
                await _notificationService.NotifyFullChargingTransactionAsync(processedData);

                logger.LogInformation("Full charging transaction processed successfully for TransactionID: {TransactionId}", processedData.TransactionId);
                return Ok(new { Status = "Success", Message = "Full charging transaction received and processed" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing full charging transaction");
                return StatusCode(500, new { Status = "Error", Message = "An error occurred while processing the full charging transaction" });
            }
        }

        // Endpoint for ChargingTransaction
        /*
        This message is send when charging transaction starts and stops. Action can be: 'transaction_start' or 'transaction_stop'
        */
        [HttpPost("charging-transaction")]
        public async Task<IActionResult> ReceiveChargingTransaction([FromBody] ChargingTransaction message, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("ReceiveChargingTransaction");

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid ModelState in ReceiveChargingTransaction");
                return BadRequest(ModelState);
            }

            try
            {
                var processedData = await _dataProcessor.ProcessChargingTransactionAsync(message);
                await _notificationService.NotifyChargingTransactionAsync(processedData);

                logger.LogInformation("Charging transaction processed successfully for TransactionID: {TransactionId}", processedData.TransactionId);
                return Ok(new { Status = "Success", Message = "Charging transaction received and processed" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing charging transaction");
                return StatusCode(500, new { Status = "Error", Message = "An error occurred while processing the charging transaction" });
            }
        }
    }
}
