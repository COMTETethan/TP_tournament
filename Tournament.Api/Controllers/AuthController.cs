using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Create a new user account and receive tokens.</summary>
    [HttpPost("auth/register")]
    [ProducesResponseType(typeof(AuthResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var auth = await _authService.RegisterAsync(request);
            return StatusCode(201, auth);
        }
        catch (EmailAlreadyRegisteredException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Authenticate with email/password and receive tokens.</summary>
    [HttpPost("auth/login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var auth = await _authService.LoginAsync(request);
            return Ok(auth);
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized("Invalid email or password.");
        }
    }

    /// <summary>Exchange a refresh token for a new token pair.</summary>
    [HttpPost("auth/refresh")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var auth = await _authService.RefreshAsync(request);
            return Ok(auth);
        }
        catch (InvalidRefreshTokenException)
        {
            return Unauthorized("Refresh token is invalid or has expired.");
        }
    }
}
