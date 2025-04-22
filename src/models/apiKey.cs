

namespace apiEndpointNameSpace.Models.ApiKey
{
    public class ApiKeyConfig
    {
        public required List<ApiKeyEntry> ValidKeys { get; set; } = new();
    }

    public class ApiKeyEntry
    {
        public required string ClientId { get; set; }
        public required string ApiKey { get; set; }
    }
}