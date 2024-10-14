using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models;

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

    public class ChargerHub : Hub
    {
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