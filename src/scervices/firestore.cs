using Google.Cloud.Firestore;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models;

namespace apiEndpointNameSpace.Services
{
    public class FirestoreService(string projectId) : IFirestoreService
    {
        private readonly FirestoreDb _db = FirestoreDb.Create(projectId);

        public async Task StoreChargerStateAsync(ProcessedChargerState data)
        {
            var docRef = _db.Collection("charger_states").Document(data.ChargerId);
            await docRef.SetAsync(data);
        }

        public async Task StoreMeasurementsAsync(ProcessedMeasurements data)
        {
            var docRef = _db.Collection("measurements").Document();
            await docRef.SetAsync(data);
        }

        public async Task<ChargerData?> GetChargerDataAsync(string chargerId)
        {
            var docRef = _db.Collection("charger_data").Document(chargerId);
            var snapshot = await docRef.GetSnapshotAsync();

            return snapshot.Exists ? snapshot.ConvertTo<ChargerData>() : null;
        }
    }
}