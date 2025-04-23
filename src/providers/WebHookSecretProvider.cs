using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using Google.Cloud.SecretManager.V1; // Add this namespace for SecretManagerServiceClient
using System.Text; // For the ToStringUtf8 extension method



public class WebhookSecretProvider : IWebhookSecretProvider
{
    private readonly string _webhookSecret;
    
    public WebhookSecretProvider(IConfiguration configuration, ILogger<WebhookSecretProvider> logger)
    {
        try
        {
            var secretClient = SecretManagerServiceClient.Create();
            string projectId = configuration["GoogleCloudProjectId"];
            string secretId = configuration["GoogleCloudSecrets:WebhookSecretId"];
            
            var secretVersionName = new SecretVersionName(projectId, secretId, "latest");
            var response = secretClient.AccessSecretVersion(secretVersionName);
            _webhookSecret = response.Payload.Data.ToStringUtf8().Trim();
            
            logger.LogInformation("Successfully loaded webhook secret at startup");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load webhook secret at startup");
            throw; // Crash the application at startup if the secret can't be loaded
        }
    }
    
    public string GetSecret() => _webhookSecret;
}

// Define the interface
public interface IWebhookSecretProvider
{
    string GetSecret();
}