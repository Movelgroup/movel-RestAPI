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
                Status = Enum.Parse<ChargerStatus>(message.Status),
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
                Measurements = message.Measurements.Select(m => new ProcessedMeasurement
                {
                    Value = decimal.Parse(m.Value),
                    TypeOfMeasurement = m.TypeOfMeasurement,
                    Phase = m.Phase,
                    Unit = m.Unit
                }).ToList()
            };

            return await Task.FromResult(processedMeasurements);
        }
    }
}