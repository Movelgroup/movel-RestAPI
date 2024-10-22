using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models;
using Microsoft.AspNetCore.Components;

namespace apiEndpointNameSpace.Services
{
    public class NotificationService(IHubContext<ChargerHub> hubContext) : INotificationService
    {

        public async Task NotifyChargerStateChangeAsync(ProcessedChargerState data)
        {
            if (data.ChargerId != null)
            {
                await hubContext.Clients.Group(data.ChargerId).SendAsync("ChargerStateChanged", data);
            }
        }

        public async Task NotifyMeasurementsUpdateAsync(ProcessedMeasurements data)
        {
            if (data.ChargerId != null)
            {
                await hubContext.Clients.Group(data.ChargerId).SendAsync("MeasurementsUpdated", data);
            }
        }

    }

    [Route("/chargerhub")]
    public class ChargerHub(ILogger<ChargerHub> logger) : Hub
    {
        private readonly ILogger<ChargerHub> _logger = logger;

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