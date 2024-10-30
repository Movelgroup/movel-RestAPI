
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using apiEndpointNameSpace.Interfaces;
using apiEndpointNameSpace.Models.ChargerData;
using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using apiEndpointNameSpace.Services;
using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Cors;


[ApiController]
[Route("api/[controller]")]
[EnableCors("CorsPolicy")] 
public class SignalRAuthController : ControllerBase
{
    private readonly ILogger<SignalRAuthController> _logger;
    private readonly IFirebaseAuthService _firebaseAuthService;

    public SignalRAuthController(
        ILogger<SignalRAuthController> logger,
        IFirebaseAuthService firebaseAuthService)
    {
        _logger = logger;
        _firebaseAuthService = firebaseAuthService;
    }

    public class SignalRConnectionRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<String> ChargerIDs { get; set; } = ["null"];
    }

    [HttpPost("connect")]
    public async Task<IActionResult> GetSignalRToken([FromBody] SignalRConnectionRequest request)
    {
        try
        {
            _logger.LogInformation("Received connection request for email: {Email}", request.Email);

            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Email and Password are required");
            }

            var authResponse = await _firebaseAuthService.AuthenticateUserAsync(request.Email, request.Password, request.ChargerIDs);

            if (!authResponse.Success)
            {
                _logger.LogWarning("Authentication failed for email: {Email}, Error: {Error}", 
                    request.Email, authResponse.ErrorMessage);
                return Unauthorized(new { message = authResponse.ErrorMessage });
            }

            _logger.LogInformation("Successfully authenticated user: {Email}", request.Email);

            return Ok(new 
            { 
                token = authResponse.Token,
                expires_in = 3600, // 1 hour
                token_type = "Bearer"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for email: {Email}", request.Email);
            return StatusCode(500, new { message = "Authentication failed", error = ex.Message });
        }
    }
}