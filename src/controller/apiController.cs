using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models.ChargerData;
using apiEndpointNameSpace.Models.Measurements;
using System.Net;
using Microsoft.AspNetCore.Cors;
using apiEndpointNameSpace.Middleware;

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
        public async Task<IActionResult> ReceiveChargerState([FromBody] ChargerStateMessage message, IServiceProvider serviceProvider)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                throw new ValidationException(string.Join("; ", errors));
            }

            var processedData = await _dataProcessor.ProcessChargerStateAsync(message);
            var storeInfo = await _firestoreService.StoreChargerStateAsync(processedData);

            return Ok(new { Status = "Success", Message = "Charger state received and processed" });
        }

        /// <summary>
        /// Receives and processes measurements reported by chargers.
        /// </summary>
        /// <param name="message">Message containing measurement data.</param>
        /// <param name="serviceProvider">Service provider for retrieving required services.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("measurements")]
        public async Task<IActionResult> ReceiveMeasurements([FromBody] MeasurementsMessage message, IServiceProvider serviceProvider)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                throw new ValidationException(string.Join("; ", errors));
            }

            var processedData = await _dataProcessor.ProcessMeasurementsAsync(message);
            await _firestoreService.StoreMeasurementsAsync(processedData);

            return Ok(new { Status = "Success", Message = "Measurements received and processed" });
        }

        /// <summary>
        /// Receives and processes a full charging transaction after completion.
        /// </summary>
        /// <param name="message">Message containing full charging transaction data.</param>
        /// <param name="serviceProvider">Service provider for retrieving required services.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("full-charging-transaction")]
        public async Task<IActionResult> ReceiveFullChargingTransaction([FromBody] FullChargingTransaction message, IServiceProvider serviceProvider)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                throw new ValidationException(string.Join("; ", errors));
            }

            var processedData = await _dataProcessor.ProcessFullChargingTransactionAsync(message);

            return Ok(new { Status = "Success", Message = "Full charging transaction received and processed" });
        }

        /// <summary>
        /// Receives and processes a charging transaction when it starts or stops.
        /// </summary>
        /// <param name="message">Message containing charging transaction data.</param>
        /// <param name="serviceProvider">Service provider for retrieving required services.</param>
        /// <returns>HTTP response indicating success or failure.</returns>
        [HttpPost("charging-transaction")]
        public async Task<IActionResult> ReceiveChargingTransaction([FromBody] ChargingTransaction message, IServiceProvider serviceProvider)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                throw new ValidationException(string.Join("; ", errors));
            }

            var processedData = await _dataProcessor.ProcessChargingTransactionAsync(message);

            return Ok(new { Status = "Success", Message = "Charging transaction received and processed" });
        }
    }
}
