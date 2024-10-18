using Google.Cloud.Firestore;
using System;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace apiEndpointNameSpace.Services
{
    public class FirestoreService : IFirestoreService
    {
        private readonly FirestoreDb _db;
        private readonly IServiceProvider _serviceProvider;

        public FirestoreService(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            var logger = _serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("FirestoreService");

            try
            {
                string projectId = configuration["GoogleCloudProjectId"] 
                    ?? throw new InvalidOperationException("GoogleCloudProjectId is not set in the configuration");

                string credentialsPath = configuration["GoogleApplicationCredentials"]
                    ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")
                    ?? throw new InvalidOperationException("Google Application Credentials path is not set");

                _db = new FirestoreDbBuilder
                {
                    ProjectId = projectId,
                    CredentialsPath = credentialsPath
                }.Build();
                
                logger.LogInformation("FirestoreService initialized successfully with project ID: {ProjectId}", projectId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize FirestoreService");
                throw;
            }
        }

        public async Task<string> StoreChargerStateAsync(ProcessedChargerState data)
        {
            var logger = _serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("FirestoreService");

            try
            {
                var docRef = _db.Collection("charger_states").Document(data.ChargerId);
                var debugInfo = $"Storing data in: {docRef.Parent.Path}, DB: {_db.ProjectId}";
                logger.LogInformation(debugInfo);
                await docRef.SetAsync(data);
                logger.LogInformation("Successfully stored charger state for ChargerId: {ChargerId}", data.ChargerId);
                return debugInfo;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to store charger state for ChargerId: {ChargerId}", data.ChargerId);
                throw;
            }
        }

        public async Task StoreMeasurementsAsync(ProcessedMeasurements data)
        {
            var logger = _serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("FirestoreService");

            try
            {
                var docRef = _db.Collection("measurements").Document();
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
            var logger = _serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("FirestoreService");

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
    }
}