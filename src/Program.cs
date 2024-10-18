using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using apiEndpointNameSpace.Services;
using apiEndpointNameSpace.Interfaces;
using NReco.Logging.File;

namespace apiEndpointNameSpace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.AddEventSourceLogger();
            builder.Logging.AddFile(builder.Configuration.GetSection("Logging"));

            // Add services to the container.
            ConfigureServices(builder.Services);

            var app = builder.Build();

            app.Logger.LogInformation("Starting web application");

            // Configure the HTTP request pipeline.
            ConfigureApp(app);

            app.Run();

        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddSingleton<IDataProcessor, DataProcessorService>();
            services.AddSingleton<IFirestoreService, FirestoreService>(); // TODO: make sure that the firebase credentials is accessable when deplaying the code, ass of now its localy stored
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IAuthorizationService, AuthorizationService>();
            services.AddSignalR();
        }

        public static void ConfigureApp(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<ChargerHub>("/chargerhub");
        }
    }
}