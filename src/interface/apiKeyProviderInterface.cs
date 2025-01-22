// IApiKeyProvider.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using apiEndpointNameSpace.Models.ApiKey;

namespace apiEndpointNameSpace.Services
{
    public interface IApiKeyProvider
    {
        
        Task<IEnumerable<ApiKeyEntry>> GetApiKeysAsync();
    }
}
