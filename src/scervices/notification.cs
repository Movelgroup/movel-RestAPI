using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models.ChargerData;
using apiEndpointNameSpace.Models.Measurements;
using apiEndpointNameSpace.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace apiEndpointNameSpace.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<ChargerHub> _hubContext;

        public NotificationService(IHubContext<ChargerHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyChargerStateChangeAsync(ProcessedChargerState data)
        {
            if (data.ChargerId != null)
            {
                await _hubContext.Clients.Group(data.ChargerId).SendAsync("ChargerStateChanged", data);
            }
        }

        public async Task NotifyMeasurementsUpdateAsync(ProcessedMeasurements data)
        {
            if (data.ChargerId != null)
            {
                await _hubContext.Clients.Group(data.ChargerId).SendAsync("MeasurementsUpdated", data);
            }
        }

        // New method for notifying FullChargingTransaction
        public async Task NotifyFullChargingTransactionAsync(ProcessedFullChargingTransaction data)
        {
            if (data.ChargerId != null)
            {
                await _hubContext.Clients.Group(data.ChargerId).SendAsync("FullChargingTransactionProcessed", data);
            }
        }

        // New method for notifying ChargingTransaction
        public async Task NotifyChargingTransactionAsync(ProcessedChargingTransaction data)
        {
            if (data.ChargerId != null)
            {
                await _hubContext.Clients.Group(data.ChargerId).SendAsync("ChargingTransactionProcessed", data);
            }
        }
    }

    [Route("/chargerhub")]
    [Authorize]
    public class ChargerHub : Hub
    {
        private readonly ILogger<ChargerHub> _logger;
        private readonly IFirebaseAuthService _firebaseAuthService;

        public ChargerHub(
        ILogger<ChargerHub> logger,
        IFirebaseAuthService firebaseAuthService)
        {
            _logger = logger;
            _firebaseAuthService = firebaseAuthService;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                _logger.LogInformation("Connection attempt from connection ID: {ConnectionId}", Context.ConnectionId);
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("User claims: {@Claims}", Context.User?.Claims.Select(c => new { c.Type, c.Value }));
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("No user ID found in claims");
                    throw new HubException("Unauthorized connection attempt");
                }

                // Get user's allowed chargers from claims
                var allowedChargers = Context.User?.Claims
                    .Where(c => c.Type == "allowedCharger")
                    .Select(c => c.Value)
                    .ToList() ?? new List<string>();

                // Join all allowed charger groups
                foreach (var chargerId in allowedChargers)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, chargerId);
                    _logger.LogInformation($"User {userId} joined charger group: {chargerId}", userId, chargerId);
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync");
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}, UserId: {userId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}