using Google.Cloud.Firestore;
using System;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models.ChargerData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using apiEndpointNameSpace.Models.Auth;
using apiEndpointNameSpace.Models.Measurements;

namespace apiEndpointNameSpace.Services
{
    public class FirestoreService : IFirestoreService
    {
        private readonly FirestoreDb _db;
        private readonly ILogger<FirestoreService> logger;

        public FirestoreService(FirestoreDb db, ILogger<FirestoreService> logger)
        {
            this.logger = logger;
            _db = db;
            logger.LogInformation("FirestoreService initialized successfully with project ID: {ProjectId}", db.ProjectId);
        }

        public async Task<string> StoreChargerStateAsync(ProcessedChargerState data)
        {

            try
            {
                var docRef = _db.Collection("charger_states").Document(data.ChargerId);
                var debugInfo = $"Storing data in: {docRef.Parent.Path}, DB: {_db.ProjectId}";
                logger.LogInformation(debugInfo);

                await docRef.SetAsync(data);

                // Store the state in the history subcollection
                var historyRef = docRef.Collection("history").Document();
                await historyRef.SetAsync(new
                {
                    Status = data.Status,
                    Timestamp = data.Timestamp
                });

                logger.LogInformation("Successfully stored charger state and history for ChargerId: {ChargerId}", data.ChargerId);
                return debugInfo;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to store charger state for ChargerId: {ChargerId}", data.ChargerId);
                throw;
            }
        }


        // Delete measurements after updated 
        public async Task StoreMeasurementsAsync(ProcessedMeasurements data)
        {
            try
            {
                var docRef = _db.Collection("charger_measurements").Document(data.ChargerId);
                await docRef.SetAsync(data);
                logger.LogInformation("Successfully stored measurements for ChargerId: {ChargerId}", data.ChargerId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to store measurements for ChargerId: {ChargerId}", data.ChargerId);
                throw;
            }
        }

        public async Task<ChargerData?> GetChargerDataAsync(string chargerId)
        {
            try
            {
                var docRef = _db.Collection("charger_data").Document(chargerId);
                var snapshot = await docRef.GetSnapshotAsync();
                if (snapshot.Exists)
                {
                    logger.LogInformation("Successfully retrieved charger data for ChargerId: {ChargerId}", chargerId);
                    return snapshot.ConvertTo<ChargerData>();
                }
                else
                {
                    logger.LogWarning("Charger data not found for ChargerId: {ChargerId}", chargerId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve charger data for ChargerId: {ChargerId}", chargerId);
                throw;
            }
        }

        public async Task StoreSlowChargingAsync(ProcessedMeasurements data, decimal powerKw)
    {
        try
        {
            var slowChargingData = new
            {
                ChargerId = data.ChargerId,
                SocketId = data.SocketId,
                Timestamp = data.Timestamp,
                PowerKw = powerKw,
                DetailedMeasurements = data.Measurements,
                DetectedAt = DateTime.UtcNow
            };

            var docRef = _db.Collection("slow_charging_incidents").Document();
            await docRef.SetAsync(slowChargingData);
            logger.LogInformation("Stored slow charging incident for ChargerId: {ChargerId}, Power: {PowerKw}kW", 
                data.ChargerId, powerKw);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to store slow charging incident for ChargerId: {ChargerId}", 
                data.ChargerId);
            throw;
        }
    }
    }
}