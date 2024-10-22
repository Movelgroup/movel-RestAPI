using System;
using System.Linq;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models;

namespace apiEndpointNameSpace.Services
{
    public class DataProcessorService : IDataProcessor
    {
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
                Measurements = message.Measurements?.Select(m => new ProcessedMeasurement
                {
                    Value = decimal.Parse(m.Value ?? throw new ArgumentNullException(nameof(message))),
                    TypeOfMeasurement = m.TypeOfMeasurement,
                    Phase = m.Phase,
                    Unit = m.Unit
                }).ToList() ?? new List<ProcessedMeasurement>()
            };

            return await Task.FromResult(processedMeasurements);
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
