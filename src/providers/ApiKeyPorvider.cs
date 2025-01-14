// SecretManagerApiKeyProvider.cs
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace apiEndpointNameSpace.Services
{
    public class SecretManagerApiKeyProvider : IApiKeyProvider
    {
        private readonly ILogger<SecretManagerApiKeyProvider> _logger;
        private readonly IConfiguration _configuration;
        private readonly SecretManagerServiceClient _secretClient;
        private readonly SecretName _secretName;
        private IEnumerable<string> _cachedApiKeys;
        private DateTime _lastFetchTime;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5); // Adjust as needed

        public SecretManagerApiKeyProvider(ILogger<SecretManagerApiKeyProvider> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _secretClient = SecretManagerServiceClient.Create();

            // Assuming the secret is named "ThirdPartyApiKeys" and stored as a JSON array
            string projectId = _configuration["GoogleCloudProjectId"];
            string secretId = _configuration["GoogleCloudSecrets:apiKeySecretId"]; // e.g., "ThirdPartyApiKeys"
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(secretId))
            {
                throw new ArgumentException("GoogleCloudProjectId or apiKeySecretId is not configured.");
            }
            
            _secretName = new SecretName(projectId, secretId);
            Secret secret = _secretClient.GetSecret(_secretName);
        }

        public async Task<IEnumerable<string>> GetApiKeysAsync()
        {
            // Return cached keys if cache is still valid
            if (_cachedApiKeys != null && DateTime.UtcNow - _lastFetchTime < _cacheDuration)
            {
                return _cachedApiKeys;
            }

            try
            {
                var request = new AccessSecretVersionRequest
                {   
                    Name = _secretName.ToString(), // Name could be wrong 
                    // Use "latest" to always get the most recent version
                    // Alternatively, specify a specific version if needed
                    // Version = "latest"
                };
                _logger.LogInformation("Secret fetch request: ",request);

                var response = await _secretClient.AccessSecretVersionAsync(request);
                string payload = response.Payload.Data.ToStringUtf8();

                // Assuming the secret is a JSON object with a "ValidKeys" array
                var apiKeyConfig = JsonConvert.DeserializeObject<ApiKeyConfig>(payload);
                _cachedApiKeys = apiKeyConfig.ValidKeys;
                _lastFetchTime = DateTime.UtcNow;

                _logger.LogInformation("Successfully fetched API keys from Secret Manager.");

                return _cachedApiKeys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message, "Failed to retrieve API keys from Secret Manager.");
                throw; // Rethrow to let the middleware handle the error appropriately
            }
        }
    }

    // ApiKeyConfig.cs
    public class ApiKeyConfig
    {
        public List<string> ValidKeys { get; set; }
    }
}
