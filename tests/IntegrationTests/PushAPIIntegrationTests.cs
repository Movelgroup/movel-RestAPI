using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using apiEndpointNameSpace.Models;

namespace apiEndpointNameSpace.Tests.IntegrationTests
{
    public class PushAPIIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public PushAPIIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task PostChargerState_ValidData_ReturnsSuccessStatus()
        {
            // Arrange
            var client = _factory.CreateClient();
            var chargerState = new ChargerStateMessage
            {
                ChargerId = "123",
                SocketId = 1,
                TimeStamp = DateTime.UtcNow,
                Status = "Available",
                ErrorCode = "None",
                Message = "Ready"
            };
            var content = new StringContent(JsonSerializer.Serialize(chargerState), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("/api/PushAPI/charger-state", content);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("Success", responseString);
        }
    }
}