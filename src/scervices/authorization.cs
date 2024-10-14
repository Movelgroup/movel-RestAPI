using System.Security.Claims;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models;

namespace apiEndpointNameSpace.Services
{
    public class AuthorizationService(IFirestoreService firestoreService) : IAuthorizationService
    {

        public async Task<bool> CanAccessChargerDataAsync(ClaimsPrincipal user, string chargerId)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return false;
            }

            if (user.IsInRole("Admin"))
            {
                return true;
            }

            var chargerData = await firestoreService.GetChargerDataAsync(chargerId);
            if (chargerData == null)
            {
                return false;
            }

            return chargerData.OwnerId == userId || (chargerData.AssociatedUserIds?.Contains(userId) ?? false);
        }
    }
}