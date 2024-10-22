using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models;
using Microsoft.AspNetCore.Mvc;

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
    public class ChargerHub : Hub
    {
        private readonly ILogger<ChargerHub> _logger;

        public ChargerHub(ILogger<ChargerHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public async Task JoinChargerGroup(string chargerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chargerId);
        }

        public async Task LeaveChargerGroup(string chargerId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chargerId);
        }
    }
}
