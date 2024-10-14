using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models;

namespace apiEndpointNameSpace.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PushAPIController(
        IDataProcessor dataProcessor,
        IFirestoreService firestoreService,
        INotificationService notificationService,
        IAuthorizationService authorizationService) : ControllerBase
    {
        [HttpPost("charger-state")]
        public async Task<IActionResult> ReceiveChargerState([FromBody] apiEndpointNameSpace.Models.ChargerStateMessage message)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var processedData = await dataProcessor.ProcessChargerStateAsync(message);
                Console.Write("recivedChargerState data recived, chargerID: ");
                Console.WriteLine(processedData.ChargerId);

                var storeInfo = await firestoreService.StoreChargerStateAsync(processedData);
                Console.WriteLine($"FirestoreInfo: {storeInfo}");

                await notificationService.NotifyChargerStateChangeAsync(processedData);
                return Ok(new { Status = "Success", Message = "Charger state received and processed", debugInfo = storeInfo });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // TODO: Log the exception
                return StatusCode(500, new { Status = "Error", Message = "An error occurred while processing the charger state" });
            }
        }

        [HttpPost("measurements")]
        public async Task<IActionResult> ReceiveMeasurements([FromBody] apiEndpointNameSpace.Models.MeasurementsMessage message)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var processedData = await dataProcessor.ProcessMeasurementsAsync(message);
                await firestoreService.StoreMeasurementsAsync(processedData);
                await notificationService.NotifyMeasurementsUpdateAsync(processedData);
                return Ok(new { Status = "Success", Message = "Measurements received and processed" });
            }
            catch (Exception)
            {
                // TODO: Log the exception
                return StatusCode(500, new { Status = "Error", Message = "An error occurred while processing the measurements" });
            }
        }

        [HttpGet("charger/{chargerId}")]
        public async Task<IActionResult> GetChargerData(string chargerId)
        {
            try
            {
                var user = HttpContext.User;
                if (!await authorizationService.CanAccessChargerDataAsync(user, chargerId))
                {
                    return Forbid();
                }

                var chargerData = await firestoreService.GetChargerDataAsync(chargerId);
                return Ok(chargerData);
            }
            catch (Exception)
            {
                // TODO: Log the exception
                return StatusCode(500, new { Status = "Error", Message = "An error occurred while retrieving charger data" });
            }
        }
    }
}