// SecretManagerApiKeyProvider.cs
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using apiEndpointNameSpace.Models.ApiKey;

namespace apiEndpointNameSpace.Services
{
    public class SecretManagerApiKeyProvider : IApiKeyProvider
    {
        private readonly ILogger<SecretManagerApiKeyProvider> _logger;
        private readonly IConfiguration _configuration;
        private readonly SecretManagerServiceClient _secretClient;
        private readonly SecretName _secretName;
        private IEnumerable<ApiKeyEntry>? _cachedApiKeys;
        private DateTime _lastFetchTime;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5); // Adjust as needed

        public SecretManagerApiKeyProvider(ILogger<SecretManagerApiKeyProvider> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _secretClient = SecretManagerServiceClient.Create();

            // Assuming the secret is named "ThirdPartyApiKeys" and stored as a JSON array
            string projectId = _configuration["GoogleCloudProjectId"]
                ?? throw new InvalidOperationException("Configuration value 'GoogleCloudProjectId' is missing or null.");
            string secretId = _configuration["GoogleCloudSecrets:apiKeySecretId"]
                ?? throw new InvalidOperationException("Configuration value 'GoogleCloudSecrets:apiKeySecretId' is missing or null.");
                            
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(secretId))
            {
                throw new ArgumentException("GoogleCloudProjectId is not configured.");
            }
            
            _secretName = new SecretName(projectId, secretId);
        }

        public async Task<IEnumerable<ApiKeyEntry>> GetApiKeysAsync()
        {
            if (_cachedApiKeys != null && DateTime.UtcNow - _lastFetchTime < _cacheDuration)
            {
                return _cachedApiKeys;
            }

            try
            {
                var secretVersionName = new SecretVersionName(_secretName.ProjectId, _secretName.SecretId, "latest");
                var request = new AccessSecretVersionRequest
                {
                    SecretVersionName = secretVersionName
                };

                // _logger.LogInformation("Fetching secrets: {SecretVersionName}", secretVersionName);

                var response = await _secretClient.AccessSecretVersionAsync(request);
                string payload = response.Payload.Data.ToStringUtf8();

                var apiKeyConfig = JsonConvert.DeserializeObject<ApiKeyConfig>(payload);
                if (apiKeyConfig == null || apiKeyConfig.ValidKeys == null)
                {
                    _logger.LogError("Invalid API key configuration.");
                    throw new FormatException("Secret payload format is incorrect.");
                }

                _cachedApiKeys = apiKeyConfig.ValidKeys;
                _lastFetchTime = DateTime.UtcNow;

                _logger.LogInformation("Successfully retrieved API keys.");
                return _cachedApiKeys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve API keys.");
                throw;
            }
        }


    }

    // ApiKeyConfig.cs
    public class ApiKeyConfig
    {
        public List<ApiKeyEntry> ValidKeys { get; set; }
    }
}
