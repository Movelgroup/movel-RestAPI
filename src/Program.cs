using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using apiEndpointNameSpace.Services;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace;

namespace apiEndpointNameSpace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = CreateHostBuilder(args).Build();
            builder.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.AddSingleton<IDataProcessor, DataProcessorService>();
            services.AddSingleton<IFirestoreService>(sp => new FirestoreService(sp.GetRequiredService<IConfiguration>()["GoogleCloudProjectId"] ?? throw new InvalidOperationException("GoogleCloudProjectId is not set in the configuration")));
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IAuthorizationService, AuthorizationService>();

            services.AddSignalR();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<ChargerHub>("/chargerhub");
            });
        }
    }
}