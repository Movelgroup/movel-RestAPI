using System.Threading.Tasks;
using System.Security.Claims;
using apiEndpointNameSpace.Models.ChargerData;
using apiEndpointNameSpace.Models.Measurements;
using apiEndpointNameSpace.Models.Auth;
using FirebaseAdmin.Auth;
using System.IdentityModel.Tokens.Jwt;



namespace apiEndpointNameSpace.Interfaces
{
    public interface IDataProcessor
    {
        Task<ProcessedChargerState> ProcessChargerStateAsync(ChargerStateMessage message);
        Task<ProcessedFullChargingTransaction> ProcessFullChargingTransactionAsync(FullChargingTransaction message);
        Task<ProcessedMeasurements> ProcessMeasurementsAsync(MeasurementsMessage message);
        Task<ProcessedChargingTransaction> ProcessChargingTransactionAsync(ChargingTransaction message);
    }

    public interface IFirestoreService
    {
        Task<string> StoreChargerStateAsync(ProcessedChargerState data);
        Task StoreMeasurementsAsync(ProcessedMeasurements data);
        Task<ChargerData?> GetChargerDataAsync(string chargerId);
        Task StoreSlowChargingAsync(ProcessedMeasurements data, decimal powerKw);
    }

    public interface INotificationService
    {
        Task NotifyChargerStateChangeAsync(ProcessedChargerState data);
        Task NotifyMeasurementsUpdateAsync(ProcessedMeasurements data);
        Task NotifyFullChargingTransactionAsync(ProcessedFullChargingTransaction data);
        Task NotifyChargingTransactionAsync(ProcessedChargingTransaction data);

    }

        public interface IFirebaseAuthService
    {
        Task<AuthResponse> AuthenticateUserMailAsync(string email, string password, List<String> chargerIDs);
        Task<AuthResponse> AuthenticateUserTokenAsync(string token, List<String> chargerIDs);

        Task<string> GenerateJwtTokenAsync(FirebaseToken decodedToken, List<string> allowedChargers);
    }

}