using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService) => _authService = authService;

    /// <summary>Return the currently authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MeResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetMe()
    {
        var sub    = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (sub is null || !int.TryParse(sub, out var userId))
            return Unauthorized();

        try
        {
            var me = await _authService.GetMeAsync(userId);
            return Ok(me);
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized();
        }
    }
}
