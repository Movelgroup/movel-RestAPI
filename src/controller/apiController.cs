using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models.ChargerData;
using apiEndpointNameSpace.Models.Measurements;
using System.Net;
using Microsoft.AspNetCore.Cors;

namespace apiEndpointNameSpace.Controllers
{

    /// <summary>
    /// API controller for handling operations related to charger data and transactions.
    /// </summary>
    [ApiController]
    [Route("EmablerChargerData")]
    public class RestAPIController : ControllerBase
    {
        private readonly IDataProcessor _dataProcessor;
        private readonly IFirestoreService _firestoreService;
        private readonly IFirebaseAuthService _firebaseAuthService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestAPIController"/> class.
        /// </summary>
        /// <param name="dataProcessor">Service for processing data related to chargers.</param>
        /// <param name="firestoreService">Service for interacting with Firestore database.</param>
        /// <param name="firebaseAuthService">Service for Firebase authentication.</param>
        public RestAPIController(
            IDataProcessor dataProcessor,
            IFirestoreService firestoreService,
            IFirebaseAuthService firebaseAuthService)
        {
            _dataProcessor = dataProcessor;
            _firestoreService = firestoreService;
            _firebaseAuthService = firebaseAuthService;
        }

        /// <summary>
        /// Receives and processes the current state of a charger.
        /// </summary>
        /// <param name="message">Message containing charger state data.</param>
        /// <param name="serviceProvider">Service provider for retrieving required services.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("charger-state")]
        // ChargerState message
        // This message is used to deliver the current status of a charging station and it’s socket Status values are: Available, Error, Offline, Info, Charging, SuspendedCAR, SuspendedCHARGER, Preparing, Finishing, Booting, Unavailable
        public async Task<IActionResult> ReceiveChargerState([FromBody] ChargerStateMessage message, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ReceiveChargerStates");
            var clientId = HttpContext.Request.Headers["X-Client-ID"].FirstOrDefault();
            logger.LogInformation("Processing request from Client ID: {ClientId}", clientId);

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
                    StackTrace = null //ex.StackTrace,
                };
                return StatusCode(500, errorResponse);
            }
        }

        /// <summary>
        /// Receives and processes measurements reported by chargers.
        /// </summary>
        /// <param name="message">Message containing measurement data.</param>
        /// <param name="serviceProvider">Service provider for retrieving required services.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("measurements")]
        // Measurements message
        // This message is send when charger is reporting meter values during the charging transaction. TypeofMeasurement, Phase and Unit are following OCPP1.6 model.
        public async Task<IActionResult> ReceiveMeasurements([FromBody] MeasurementsMessage message, IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("ReceiveMeasurements");

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid ModelState in ReceiveMeasurements: {@ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                logger.LogInformation("ReceiveMeasurements: Received message: {@Message}", message);

                var processedData = await _dataProcessor.ProcessMeasurementsAsync(message);

                logger.LogInformation("ReceiveMeasurements: Processed data: {@ProcessedData}", processedData);

                await _firestoreService.StoreMeasurementsAsync(processedData);
                logger.LogInformation("Measurements stored successfully for ChargerID: {ChargerId}", processedData.ChargerId);

                return Ok(new { Status = "Success", Message = "Measurements received and processed" });
            }
            catch (ArgumentException ex)
            {
                logger.LogError(ex, "Validation error in ReceiveMeasurements: {Message}", ex.Message);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ReceiveMeasurements: {Message}", ex.Message);
                return StatusCode(500, new { Status = "Error", Message = "An error occurred while processing the measurements" });
            }
        }

        /// <summary>
        /// Receives and processes a full charging transaction after completion.
        /// </summary>
        /// <param name="message">Message containing full charging transaction data.</param>
        /// <param name="serviceProvider">Service provider for retrieving required services.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("full-charging-transaction")]
        // Endpoint for FullChargingTransaction
        // This message is used to deliver information of a full charging transaction. This message is delivered after the charging has ended.
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

                logger.LogInformation("Full charging transaction processed successfully for TransactionID: {TransactionId}", processedData.TransactionId);
                return Ok(new { Status = "Success", Message = "Full charging transaction received and processed" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing full charging transaction");
                return StatusCode(500, new { Status = "Error", Message = "An error occurred while processing the full charging transaction" });
            }
        }

        /// <summary>
        /// Receives and processes a charging transaction when it starts or stops.
        /// </summary>
        /// <param name="message">Message containing charging transaction data.</param>
        /// <param name="serviceProvider">Service provider for retrieving required services.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("charging-transaction")]
        // Endpoint for ChargingTransaction
        // This message is send when charging transaction starts and stops. Action can be: 'transaction_start' or 'transaction_stop'
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
