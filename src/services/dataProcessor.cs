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
            // Assuming message non-null check happened before calling
            if (string.IsNullOrEmpty(message.ChargerId)) // Still good to validate required fields
            {
                throw new ArgumentException("ChargerId is required for ChargerStateMessage");
            }
            var processedState = new ProcessedChargerState
            {
                ChargerId = message.ChargerId, // Safe due to check above
                SocketId = message.SocketId,
                Timestamp = message.TimeStamp.ToUniversalTime(), // Convert to UTC
                Status = message.Status ?? "UNKNOWN", // Provide default if Status can be null
                MessageType = "chargerState",
                Message = message.Message ?? string.Empty // Provide default if Message can be null
            };
            return await Task.FromResult(processedState);
        }

        public async Task<ProcessedMeasurements> ProcessMeasurementsAsync(MeasurementsMessage message)
        {
            try
            {
                if (string.IsNullOrEmpty(message.ChargerId))
                {
                    _logger.LogError("ProcessMeasurementsAsync: ChargerId is required but was null or empty.");
                    throw new ArgumentException("ChargerId is required");
                }

                _logger.LogInformation("ProcessMeasurementsAsync: Received MeasurementsMessage with ChargerId: {ChargerId}, SocketId: {SocketId}, Timestamp: {Timestamp}",
                    message.ChargerId, message.SocketId, message.TimeStamp);

                // Initialize ProcessedMeasurements
                var processedMeasurements = new ProcessedMeasurements
                {
                    ChargerId = message.ChargerId,
                    SocketId = message.SocketId,
                    Timestamp = message.TimeStamp,
                    MessageType = "measurements",
                    Measurements = message.Measurements?.Select(m =>
                    {
                        _logger.LogInformation("Processing measurement: Value: {Value}, Type: {TypeOfMeasurement}, Phase: {Phase}, Unit: {Unit}",
                            m.Value, m.TypeOfMeasurement, m.Phase, m.Unit);

                        if (!decimal.TryParse(m.Value, out var parsedValue))
                        {
                            _logger.LogError("Invalid measurement value: {Value}. Skipping this measurement.", m.Value);
                            // Optionally, throw an exception or handle the error differently
                            throw new FormatException($"Invalid measurement value: {m.Value}");
                        }

                        return new ProcessedMeasurement
                        {
                            Value = parsedValue,
                            TypeOfMeasurement = m.TypeOfMeasurement,
                            Phase = m.Phase ?? string.Empty,
                            Unit = m.Unit ?? string.Empty
                        };
                    }).ToList() ?? new List<ProcessedMeasurement>()
                };

                _logger.LogInformation("ProcessedMeasurements created: {ProcessedMeasurements}", processedMeasurements);

                return processedMeasurements;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Validation error in ProcessMeasurementsAsync: {Message}", ex.Message);
                throw;
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Error parsing Measurement value in ProcessMeasurementsAsync. Message: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ProcessMeasurementsAsync: {Message}", ex.Message);
                throw;
            }
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
