
using apiEndpointNameSpace.Services;
using apiEndpointNameSpace.Interfaces;
using NReco.Logging.File;
using Newtonsoft.Json.Linq;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.SecretManager.V1;
using System.Security.Principal;

namespace apiEndpointNameSpace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(int.Parse(port));
            });

            // Configure logging
            ConfigureLogging(builder);

            var firestoreDb = InitializeFirestoreDb(builder.Configuration);
            InitializeFirebaseAuth(builder.Configuration);

            // Add services to the container.
            ConfigureServices(builder.Services, firestoreDb, builder.Configuration);



            var app = builder.Build();

            app.Logger.LogInformation("Starting web application on port {port}", port);

            // Configure the HTTP request pipeline.
            ConfigureApp(app);



            app.Run();

        }

        private static FirestoreDb InitializeFirestoreDb(IConfiguration configuration)
        {
            if (FirebaseApp.DefaultInstance != null)
            {
                Console.WriteLine("FirebaseApp already initialized.");
            }
            else
            {
                bool isCloudRun = Environment.GetEnvironmentVariable("K_SERVICE") != null;

                if (isCloudRun)
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        ProjectId = configuration["GoogleCloudProjectId"]
                    });
                    Console.WriteLine("Firebase initialized with default credentials.");
                }
                else
                {
                    string? credentialsPath = configuration["firebaseServiceAccount"];
                    if (!string.IsNullOrEmpty(credentialsPath))
                    {
                        var credential = GoogleCredential.FromFile(credentialsPath);
                        FirebaseApp.Create(new AppOptions
                        {
                            Credential = credential,
                            ProjectId = configuration["GoogleCloudProjectId"]
                        });
                        Console.WriteLine("Firebase initialized successfully with service account credentials.");
                    }
                    else
                    {
                        throw new InvalidOperationException("Firebase credentials file not found or path not configured.");
                    }
                }
            }

            // Create and return the FirestoreDb instance
            string projectId = configuration["GoogleCloudProjectId"]
                ?? throw new InvalidOperationException("GoogleCloudProjectId is not set");

            return FirestoreDb.Create(projectId);
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
            if (FirebaseApp.DefaultInstance != null) return;

            // Check if running in Cloud Run by verifying the presence of the K_SERVICE environment variable
            bool isCloudRun = Environment.GetEnvironmentVariable("K_SERVICE") != null;
            var credential = GoogleCredential.GetApplicationDefault();
            Console.WriteLine($"Default credentials: {credential}");

            if (isCloudRun)
            {
                // Use default credentials provided by Cloud Run's service account
                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential,
                    ProjectId = configuration["GoogleCloudProjectId"]
                });
                Console.WriteLine("Firebase initialized with default credentials.");
                return;
            }

            // Local development with service account file
            string? credentialsPath = configuration["firebaseServiceAccount"];
            Console.WriteLine($"Service account path: {credentialsPath}");

            if (!string.IsNullOrEmpty(credentialsPath))
            {
                try
                {
                    var credentialLocal = GoogleCredential.FromFile(credentialsPath);
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = credentialLocal,
                        ProjectId = configuration["GoogleCloudProjectId"]
                    });
                    Console.WriteLine("Firebase initialized successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading credentials: {ex.Message}");
                    throw;
                }
                return;
            }

            throw new InvalidOperationException("Firebase credentials file not found or path not configured.");
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
                    // Policy for regular API endpoints 
                    options.AddPolicy("ApiPolicy", builder =>
                    {
                        builder
                            .WithOrigins(
                                "http://localhost:3000",
                                "https://movelsoftwaremanager.web.app",
                                "https://movelsoftwaremanager.firebaseapp.com")
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()  
                            .WithExposedHeaders("Content-Disposition")
                            .WithHeaders("Authorization", "Content-Type", "Accept");
                    });

                    // Separate policy for SignalR
                    options.AddPolicy("SignalRPolicy", builder =>
                    {
                        builder
                            .WithOrigins(
                                "http://localhost:3000",
                                "https://movelsoftwaremanager.web.app",
                                "https://movelsoftwaremanager.firebaseapp.com")
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()  // Required for SignalR
                            .WithHeaders(
                                "X-Requested-With",
                                "Content-Type",
                                "Accept",
                                "Authorization",
                                "x-signalr-user-agent",  // Add this specific header
                                "x-requested-with",      // Sometimes needed in lowercase
                                "x-signalr-protocol"     // Add this SignalR specific header
                            )
                            .WithExposedHeaders(
                                        "Negotiate",
                                        "X-SignalR-Protocol",
                                        "X-SignalR-Version",
                                        "X-SignalR-Error"
                            );
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
            services.AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = true;
                hubOptions.HandshakeTimeout = TimeSpan.FromSeconds(30);
            }).AddJsonProtocol();
        }


        public static void ConfigureApp(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseWebSockets();

            app.UseEndpoints(endpoints =>
                {
                    
                    // Apply specific CORS policies to different endpoints
                    endpoints.MapControllers()
                            .RequireCors("ApiPolicy");

                    
                    endpoints.MapHub<ChargerHub>("/chargerhub")
                            .RequireCors("SignalRPolicy");
                    
                    endpoints.MapGet("/health", () => "Healthy")
                            .RequireCors("ApiPolicy");
                }); 

            app.Logger.LogInformation("SignalR Hub mapped at: /chargerhub");
        }
    }
}