using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/players")]
public class PlayersController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayersController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    /// <summary>Create a champion owned by the current user.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PlayerResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreatePlayer([FromBody] CreatePlayerRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var created = await _playerService.CreatePlayerAsync(userId, request);
            return CreatedAtAction(nameof(GetPlayer), new { id = created.Id }, created);
        }
        catch (ClassNotFoundException ex) { return NotFound(ex.Message); }
        catch (ArgumentException ex)      { return BadRequest(ex.Message); }
    }

    /// <summary>Get a champion by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PlayerResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayer(int id)
    {
        try { return Ok(await _playerService.GetPlayerAsync(id)); }
        catch (PlayerNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>List the current user's champions.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<PlayerResponse>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetMyPlayers()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        return Ok(await _playerService.GetUserPlayersAsync(userId));
    }

    private bool TryGetUserId(out int userId)
    {
        userId = 0;
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
               ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return sub is not null && int.TryParse(sub, out userId);
    }
}
