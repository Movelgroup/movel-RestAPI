using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using apiEndpointNameSpace.Interfaces;

namespace apiEndpointNameSpace.Tests.IntegrationTests
{
    public class CustomWebApplicationFactory<TProgram>
        : WebApplicationFactory<TProgram> where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IFirestoreService>(new Mock<IFirestoreService>().Object);
                // Add other mocked services as needed
            });
        }
    }
}