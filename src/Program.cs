
using apiEndpointNameSpace.Services;
using apiEndpointNameSpace.Interfaces;
using NReco.Logging.File;
using Google.Cloud.Firestore;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


namespace apiEndpointNameSpace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging
            ConfigureLogging(builder);

            var firestoreDb = InitializeFirestoreDb(builder.Configuration);

            // Add services to the container.
            ConfigureServices(builder.Services, firestoreDb);

            var app = builder.Build();

            app.Logger.LogInformation("Starting web application");

            // Configure the HTTP request pipeline.
            ConfigureApp(app);

            app.Run();

        }

        private static FirestoreDb InitializeFirestoreDb(IConfiguration configuration)
        {
            string projectId = configuration["GoogleCloudProjectId"]
                ?? throw new InvalidOperationException("GoogleCloudProjectId is not set in the configuration");

            string credentialsPath = configuration["GoogleApplicationCredentials"]
                ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")
                ?? throw new InvalidOperationException("Google Application Credentials path is not set");

            Console.WriteLine(projectId);
            Console.WriteLine(credentialsPath);
            JObject parsed = JObject.Parse(File.ReadAllText(credentialsPath));

            foreach (var pair in parsed)
        {
            Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
        }


            return new FirestoreDbBuilder
            {
                ProjectId = projectId,
                CredentialsPath = credentialsPath
            }.Build();
        }


        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.AddEventSourceLogger();
            builder.Logging.AddFile(builder.Configuration.GetSection("Logging"));
        }


        public static void ConfigureServices(IServiceCollection services, FirestoreDb firestoreDb)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddCors(options =>
                {
                    options.AddPolicy("CorsPolicy", builder =>
                    {
                        builder
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                            .WithOrigins("http://localhost:3000"); // Replace with actual frontend URL
                    });
                });

            services.AddSwaggerGen();
            services.AddSingleton<IDataProcessor, DataProcessorService>();
            services.AddSingleton<IFirestoreService>(sp => 
            {
                var logger = sp.GetRequiredService<ILogger<FirestoreService>>();
                return new FirestoreService(firestoreDb, logger);
            });
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IAuthorizationService, AuthorizationService>();
            services.AddSignalR(option =>
            {
                option.EnableDetailedErrors = true;
            });

            
        }


        public static void ConfigureApp(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("CorsPolicy");
            app.UseRouting();
            app.UseWebSockets();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapHub<ChargerHub>("/chargerhub");
            app.MapControllers();

            app.Logger.LogInformation("SignalR Hub mapped at: /chargerhub");
        }
    }
}