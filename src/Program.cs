
using apiEndpointNameSpace.Services;
using apiEndpointNameSpace.Interfaces;
using NReco.Logging.File;
using Google.Cloud.Firestore;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;


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
            InitializeFirebaseAuth(builder.Configuration);

            // Add services to the container.
            ConfigureServices(builder.Services, firestoreDb, builder.Configuration);

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

        private static void InitializeFirebaseAuth(IConfiguration configuration)
        {
            // Check if Firebase is already initialized
            if (FirebaseApp.DefaultInstance == null)
            {
                string credentialsPath = configuration["GoogleApplicationCredentials"]
                    ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS")
                    ?? throw new InvalidOperationException("Google Application Credentials path is not set");

                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialsPath),
                    ProjectId = configuration["GoogleCloudProjectId"]
                });
            }
        }



        public static void ConfigureServices(IServiceCollection services, FirestoreDb firestoreDb, IConfiguration configuration)
        {
            // Add this logging at the start to debug configuration
            var jwtKey = configuration["Jwt:Key"];
            var jwtIssuer = configuration["Jwt:Issuer"];
            var jwtAudience = configuration["Jwt:Audience"];
            
            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            {
                throw new InvalidOperationException($"JWT Configuration missing. Key: {jwtKey != null}, Issuer: {jwtIssuer != null}, Audience: {jwtAudience != null}");
            }

            services.AddSingleton(firestoreDb);
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddCors(options =>
                {
                    options.AddPolicy("CorsPolicy", builder =>
                    {
                        builder
                            .WithOrigins("http://localhost:3000", "https://movelsoftwaremanager.web.app") // Replace with actual frontend URL
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                            .SetIsOriginAllowed(_ => true); // TODO: remove in production
                    });
                });

            // Configure JWT Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured")))
                };

                // Configure JWT Bearer events for SignalR
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chargerhub"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
            services.AddSwaggerGen();
            services.AddSingleton<IDataProcessor, DataProcessorService>();
            services.AddSingleton<IFirestoreService>(sp => 
            {
                var logger = sp.GetRequiredService<ILogger<FirestoreService>>();
                return new FirestoreService(firestoreDb, logger);
            });
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
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

            app.UseRouting();
            app.UseCors("CorsPolicy");

            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseWebSockets();
            app.UseHttpsRedirection();

            app.MapControllers();
            app.MapHub<ChargerHub>("/chargerhub")
                .RequireCors("CorsPolicy");

            app.Logger.LogInformation("SignalR Hub mapped at: /chargerhub");
        }
    }
}