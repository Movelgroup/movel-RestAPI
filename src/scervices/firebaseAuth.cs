using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using System.Security.Claims;
using apiEndpointNameSpace.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using apiEndpointNameSpace.Models.Auth;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace apiEndpointNameSpace.Services
{
    public class FirebaseAuthService : IFirebaseAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FirebaseAuthService> _logger;
        private readonly FirebaseAuth _firebaseAuth;
        private readonly FirestoreDb _firestoreDb;

        public FirebaseAuthService(
            IConfiguration configuration,
            ILogger<FirebaseAuthService> logger,
            FirestoreDb firestoreDb)
        {
            _configuration = configuration;
            _logger = logger;
            _firestoreDb = firestoreDb;
            _firebaseAuth = FirebaseAuth.DefaultInstance;
        }

        public async Task<AuthResponse> AuthenticateUserAsync(string email, string password, List<String> chargerIDs)
        {
            try
            {
                _logger.LogInformation("Starting authentication for email: {Email}", email);

                if (_firebaseAuth == null)
                {
                    _logger.LogError("FirebaseAuth is null - Firebase not properly initialized");
                    return new AuthResponse 
                    { 
                        Success = false, 
                        ErrorMessage = "Firebase authentication not initialized" 
                    };
                }

                // Check JWT configuration
                var jwtKey = _configuration["Jwt:Key"];
                var jwtIssuer = _configuration["Jwt:Issuer"];
                var jwtAudience = _configuration["Jwt:Audience"];

                if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
                {
                    _logger.LogError("JWT configuration missing. Key: {KeyExists}, Issuer: {IssuerExists}, Audience: {AudienceExists}",
                        !string.IsNullOrEmpty(jwtKey),
                        !string.IsNullOrEmpty(jwtIssuer),
                        !string.IsNullOrEmpty(jwtAudience));
                    
                    throw new InvalidOperationException("JWT configuration is incomplete");
                }

                // First, verify the user exists in Firebase
                var userRecord = await _firebaseAuth.GetUserByEmailAsync(email);
                
                if (userRecord == null)
                {
                    return new AuthResponse 
                    { 
                        Success = false, 
                        ErrorMessage = "User not found" 
                    };
                }

                // Instead of verifying the custom token, we'll use the user's UID directly
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userRecord.Uid),
                    new Claim(ClaimTypes.Email, userRecord.Email ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // Add allowed chargers as claims
                claims.AddRange(chargerIDs.Select(chargerId => 
                new Claim("allowedCharger", chargerId)));

                // Generate our application's JWT token directly
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured")));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var tokenLifetimeHours = _configuration.GetValue<int>("Jwt:TokenLifetimeHours", 24);

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(tokenLifetimeHours),
                    signingCredentials: credentials
                );

                 var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

                return new AuthResponse
                {
                    Success = true,
                    Token = jwtToken,
                    UserId = userRecord.Uid,
                    Email = email,
                    AllowedChargers = chargerIDs
                };
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError(ex, "Firebase authentication failed for email: {Email}", email);
                return new AuthResponse
                {
                    Success = false,
                    ErrorMessage = $"Authentication failed: {ex.Message}"
                };
            }
        }


        public async Task<string> GenerateJwtTokenAsync(FirebaseToken decodedToken, List<string> allowedChargers)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, decodedToken.Uid),
                new Claim(ClaimTypes.Email, decodedToken.Claims["email"].ToString() ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add allowed chargers as claims
            claims.AddRange(allowedChargers.Select(chargerId => 
                new Claim("allowedCharger", chargerId)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured")));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenLifetimeHours = _configuration.GetValue<int>("Jwt:TokenLifetimeHours", 24);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(tokenLifetimeHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}