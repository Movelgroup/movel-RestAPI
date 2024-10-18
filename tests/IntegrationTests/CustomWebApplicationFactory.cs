using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace;
using NReco.Logging.File;
using System.IO;

namespace apiEndpointNameSpace.Tests.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
            });

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IFirestoreService>(new Mock<IFirestoreService>().Object);
                // Add other mocked services as needed
            });

            builder.ConfigureLogging((hostingContext, loggingBuilder) =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                loggingBuilder.AddFile(hostingContext.Configuration.GetSection("Logging:File"));
            });
        }
    }
}