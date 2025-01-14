// IApiKeyProvider.cs
using System.Collections.Generic;
using System.Threading.Tasks;

namespace apiEndpointNameSpace.Services
{
    public interface IApiKeyProvider
    {
        Task<IEnumerable<string>> GetApiKeysAsync();
    }
}
