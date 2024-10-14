using Xunit;
using apiEndpointNameSpace.Services;
using apiEndpointNameSpace.Models;
using System;
using System.Threading.Tasks;

namespace apiEndpointNameSpace.Tests.UnitTests
{
    public class DataProcessorTests
    {
        [Fact]
        public async Task ProcessChargerStateAsync_ValidInput_ReturnsProcessedData()
        {
            // Arrange
            var dataProcessor = new DataProcessorService();
            var input = new ChargerStateMessage
            {
                ChargerId = "123",
                SocketId = 1,
                TimeStamp = DateTime.UtcNow,
                Status = "Available",
                ErrorCode = "None",
                Message = "Ready"
            };

            // Act
            var result = await dataProcessor.ProcessChargerStateAsync(input);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(input.ChargerId, result.ChargerId);
            Assert.Equal(ChargerStatus.Available, result.Status);
        }
    }
}