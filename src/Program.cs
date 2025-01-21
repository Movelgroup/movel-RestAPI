
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
using apiEndpointNameSpace.Middleware;
using apiEndpointNameSpace.Models.ApiKey;
using Newtonsoft.Json;

namespace apiEndpointNameSpace
{   
    /// <summary>
    /// Main entry point for the application.
    /// Configures and starts the web application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Application's main method.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
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

        /// <summary>
        /// Initializes FirestoreDb instance.
        /// </summary>
        /// <param name="configuration">Application configuration object.</param>
        /// <returns>Configured FirestoreDb instance.</returns>
        private static FirestoreDb InitializeFirestoreDb(IConfiguration configuration)
        {
            if (FirebaseApp.DefaultInstance != null)
            {
                Console.WriteLine("FirebaseApp already initialized.");
            }
            else
            {
                bool isCloudRun = Environment.GetEnvironmentVariable("K_SERVICE") != null;
                var credential = GoogleCredential.GetApplicationDefault();
                Console.WriteLine($"Default credentials: {credential}");
                
                if (isCloudRun)
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = credential,
                        ProjectId = configuration["GoogleCloudProjectId"]
                    });
                    Console.WriteLine("Firebase initialized with default credentials.");
                }
                else
                {
                    string? credentialsPath = configuration["firebaseServiceAccount"];
                    if (!string.IsNullOrEmpty(credentialsPath))
                    {
                        var credentialLocal = GoogleCredential.FromFile(credentialsPath);
                        FirebaseApp.Create(new AppOptions
                        {
                            Credential = credentialLocal,
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

 
        /// <summary>
        /// Configures logging for the application.
        /// </summary>
        /// <param name="builder">Web application builder object.</param>
        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            builder.Logging.AddEventSourceLogger();
            builder.Logging.AddFile(builder.Configuration.GetSection("Logging"));
        }

        /// <summary>
        /// Initializes Firebase authentication.
        /// </summary>
        /// <param name="configuration">Application configuration object.</param>
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


        /// <summary>
        /// Configures services for dependency injection.
        /// </summary>
        /// <param name="services">Service collection for DI.</param>
        /// <param name="firestoreDb">Initialized FirestoreDb instance.</param>
        /// <param name="configuration">Application configuration object.</param>
        public static void ConfigureServices(IServiceCollection services, FirestoreDb firestoreDb, IConfiguration configuration)
        {
            // Register the SecretManagerApiKeyProvider as a singleton
            services.AddSingleton<IApiKeyProvider, SecretManagerApiKeyProvider>();
            
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
                                "https://movelsoftwaremanager.firebaseapp.com",
                                "https://swagger-ui-service-390725443005.europe-west1.run.app",
                                "https://movel-restapi-390725443005.europe-west1.run.app"
                                )
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
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

                //TODO: remove this section
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

            services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                    {
                        Title = "Movel RestAPI",
                        Version = "v1",
                        Description = "API documentation for internal and third-party.",
                        Contact = new Microsoft.OpenApi.Models.OpenApiContact
                            {
                                Name = "Theo Magnor",
                                Email = "theo@movel.no",
                            }
                    });

                    // Add the API Key security definition
                    c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Description = "API Key required. Enter your API key in the 'x-api-key' header.",
                        Name = "x-api-key", // Header name used for the API key
                        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    });

                    // Apply the API Key requirement globally or to specific endpoints
                    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                    {
                        {
                            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                            {
                                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                                {
                                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                    Id = "ApiKey"
                                }
                            },
                            new string[] { } // No specific scopes required for API keys
                        }
                    });

                    // Include XML comments (optional but recommended for detailed docs)
                    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                });

            services.AddSingleton<IDataProcessor, DataProcessorService>();
            services.AddSingleton<IFirestoreService>(sp => 
            {
                var logger = sp.GetRequiredService<ILogger<FirestoreService>>();
                return new FirestoreService(firestoreDb, logger);
            });
            services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
        }


        /// <summary>
        /// Configures the middleware and request pipeline for the application.
        /// </summary>
        /// <param name="app">Web application instance.</param>
        public static void ConfigureApp(WebApplication app)
        {
            app.UseRouting();
            app.UseCors("ApiPolicy");

            // Enable middleware to serve generated Swagger as JSON endpoint
            app.UseSwagger();
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                c.RoutePrefix = string.Empty; // Set Swagger UI at application's root, optional. 
            });

            app.UseApiKeyMiddleware();

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseWebSockets();

            app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers().RequireCors("ApiPolicy"); // Apply the CORS policy
                    endpoints.MapGet("/health", () => "Healthy").RequireCors("ApiPolicy");
                }); 

        }
    }
}