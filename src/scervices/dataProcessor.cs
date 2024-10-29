using System;
using System.Linq;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models.ChargerData;
using apiEndpointNameSpace.Models.Measurements;

namespace apiEndpointNameSpace.Services
{
    public class DataProcessorService : IDataProcessor
    {

        private const decimal SLOW_CHARGING_THRESHOLD_KW = 1.0m; // 1 kW threshold
        private readonly IFirestoreService _firestoreService;
        private readonly ILogger<DataProcessorService> _logger;
        public DataProcessorService(
        IFirestoreService firestoreService,
        ILogger<DataProcessorService> logger)
    {
        _firestoreService = firestoreService;
        _logger = logger;
    }


        public async Task<ProcessedChargerState> ProcessChargerStateAsync(ChargerStateMessage message)
        {
            if (string.IsNullOrEmpty(message.ChargerId))
            {
                throw new ArgumentException("ChargerId is required");
            }

            var processedState = new ProcessedChargerState
            {
                ChargerId = message.ChargerId,
                SocketId = message.SocketId,
                Timestamp = message.TimeStamp,
                Status = message.Status,
                ErrorCode = message.ErrorCode,
                MessageType = "chargerState",
                Message = message.Message
            };

            return await Task.FromResult(processedState);
        }

        public async Task<ProcessedMeasurements> ProcessMeasurementsAsync(MeasurementsMessage message)
        {
            if (string.IsNullOrEmpty(message.ChargerId))
            {
                throw new ArgumentException("ChargerId is required");
            }

            var processedMeasurements = new ProcessedMeasurements
            {
                ChargerId = message.ChargerId,
                SocketId = message.SocketId,
                Timestamp = message.TimeStamp,
                MessageType = "measurements",
                Measurements = message.Measurements?.Select(m => new ProcessedMeasurement
                {
                    Value = decimal.Parse(m.Value ?? throw new ArgumentNullException(nameof(message))),
                    TypeOfMeasurement = m.TypeOfMeasurement,
                    Phase = m.Phase,
                    Unit = m.Unit
                }).ToList() ?? new List<ProcessedMeasurement>()
            };

            // Check for slow charging
            var powerMeasurements = processedMeasurements.Measurements
                .Where(m => m.TypeOfMeasurement?.ToLower() == "power" && 
                        m.Unit?.ToLower() == "kw")
                .ToList();

            if (powerMeasurements.Any())
            {
                // Calculate total power across all phases
                decimal totalPowerKw = powerMeasurements.Sum(m => m.Value ?? 0);

                if (totalPowerKw < SLOW_CHARGING_THRESHOLD_KW)
                {
                    _logger.LogWarning(
                        "Detected slow charging for ChargerId: {ChargerId}, Power: {PowerKw}kW",
                        message.ChargerId, totalPowerKw);

                    try
                    {
                        await _firestoreService.StoreSlowChargingAsync(processedMeasurements, totalPowerKw);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't throw - we don't want to interrupt the main flow
                        _logger.LogError(ex, 
                            "Failed to store slow charging incident for ChargerId: {ChargerId}",
                            message.ChargerId);
                    }
                }
            }

            return processedMeasurements;
        }

        // New method for processing FullChargingTransaction
        public async Task<ProcessedFullChargingTransaction> ProcessFullChargingTransactionAsync(FullChargingTransaction message)
        {
            if (string.IsNullOrEmpty(message.ChargerId))
            {
                throw new ArgumentException("ChargerId is required");
            }

            var processedTransaction = new ProcessedFullChargingTransaction
            {
                ChargerId = message.ChargerId,
                MessageType = "fullyCharged",
                SocketId = message.SocketId,
                TimeStampStart = message.TimeStampStart,
                TimeStampEnd = message.TimeStampEnd,
                TransactionId = message.TransactionId,
                AuthorizedIdTag = message.AuthorizedIdTag,
                MeterReadStart = message.MeterReadStart,
                MeterReadEnd = message.MeterReadEnd,
                ConsumptionWh = message.ConsumptionWh
            };

            return await Task.FromResult(processedTransaction);
        }

        // New method for processing ChargingTransaction
        public async Task<ProcessedChargingTransaction> ProcessChargingTransactionAsync(ChargingTransaction message)
        {
            if (string.IsNullOrEmpty(message.ChargerId))
            {
                throw new ArgumentException("ChargerId is required");
            }

            var processedTransaction = new ProcessedChargingTransaction
            {
                ChargerId = message.ChargerId,
                MessageType = "chargingStartStop",
                SocketId = message.SocketId,
                TimeStamp = message.TimeStamp,
                Action = message.Action,
                TransactionId = message.TransactionId,
                AuthorizedIdTag = message.AuthorizedIdTag,
                MeterRead = message.MeterRead
            };

            return await Task.FromResult(processedTransaction);
        }
    }
}
