using System.Threading.Tasks;
using System.Security.Claims;
using apiEndpointNameSpace.Models;

namespace apiEndpointNameSpace.Interfaces
{
    public interface IDataProcessor
    {
        Task<ProcessedChargerState> ProcessChargerStateAsync(ChargerStateMessage message);
        Task<ProcessedMeasurements> ProcessMeasurementsAsync(MeasurementsMessage message);
    }

    public interface IFirestoreService
    {
        Task<string> StoreChargerStateAsync(ProcessedChargerState data);
        Task StoreMeasurementsAsync(ProcessedMeasurements data);
        Task<ChargerData?> GetChargerDataAsync(string chargerId);
    }

    public interface INotificationService
    {
        Task NotifyChargerStateChangeAsync(ProcessedChargerState data);
        Task NotifyMeasurementsUpdateAsync(ProcessedMeasurements data);
    }

    public interface IAuthorizationService
    {
        Task<bool> CanAccessChargerDataAsync(ClaimsPrincipal user, string chargerId);
    }
}