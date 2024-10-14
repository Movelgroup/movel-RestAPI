/*
This script defines a custom WebApplicationFactory that sets up the testing environment. 
It's responsible for configuring the test server with mocked services.
*/

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace;

namespace apiEndpointNameSpace.Tests.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
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