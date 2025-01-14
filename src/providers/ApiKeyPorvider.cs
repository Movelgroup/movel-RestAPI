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
                // Construct the secret version name using "latest"
                var secretVersionName = new SecretVersionName(_secretName.ProjectId, _secretName.SecretId, "latest");
                var request = new AccessSecretVersionRequest
                {
                    SecretVersionName = secretVersionName
                };

                _logger.LogInformation("Secret fetch request for: {SecretVersionName}", secretVersionName);

                var response = await _secretClient.AccessSecretVersionAsync(request);
                string payload = response.Payload.Data.ToStringUtf8();

                var apiKeyConfig = JsonConvert.DeserializeObject<ApiKeyConfig>(payload);
                if (apiKeyConfig == null || apiKeyConfig.ValidKeys == null)
                {
                    _logger.LogError("Failed to deserialize secret payload into ApiKeyConfig.");
                    throw new FormatException("Secret payload format is incorrect.");
                }

                _cachedApiKeys = apiKeyConfig.ValidKeys;
                _lastFetchTime = DateTime.UtcNow;

                _logger.LogInformation("Successfully fetched API keys from Secret Manager.");
                return _cachedApiKeys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve API keys from Secret Manager.");
                throw;
            }
        }
    }

    // ApiKeyConfig.cs
    public class ApiKeyConfig
    {
        public List<string> ValidKeys { get; set; }
    }
}
