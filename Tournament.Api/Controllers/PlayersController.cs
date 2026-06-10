using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
public class PlayersController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayersController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    /// <summary>Add a player to a tournament.</summary>
    [HttpPost("api/tournaments/{tournamentId:int}/players")]
    [ProducesResponseType(typeof(PlayerResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddPlayer(int tournamentId, [FromBody] CreatePlayerRequest request)
    {
        try
        {
            var created = await _playerService.AddPlayerAsync(tournamentId, request);
            return CreatedAtAction(nameof(GetPlayer), new { id = created.Id }, created);
        }
        catch (TournamentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>List all players in a tournament.</summary>
    [HttpGet("api/tournaments/{tournamentId:int}/players")]
    [ProducesResponseType(typeof(IEnumerable<PlayerResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTournamentPlayers(int tournamentId)
    {
        try
        {
            var players = await _playerService.GetTournamentPlayersAsync(tournamentId);
            return Ok(players);
        }
        catch (TournamentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Get a player by id.</summary>
    [HttpGet("api/players/{id:int}")]
    [ProducesResponseType(typeof(PlayerResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayer(int id)
    {
        try
        {
            var player = await _playerService.GetPlayerAsync(id);
            return Ok(player);
        }
        catch (PlayerNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Disqualify a player (score reset to 0).</summary>
    [HttpPost("api/players/{id:int}/disqualify")]
    [ProducesResponseType(typeof(PlayerResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DisqualifyPlayer(int id)
    {
        try
        {
            var player = await _playerService.DisqualifyPlayerAsync(id);
            return Ok(player);
        }
        catch (PlayerNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Add penalty points to a player.</summary>
    [HttpPatch("api/players/{id:int}/penalties")]
    [ProducesResponseType(typeof(PlayerResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddPenalty(int id, [FromBody] AddPenaltyRequest request)
    {
        try
        {
            var player = await _playerService.AddPenaltyAsync(id, request);
            return Ok(player);
        }
        catch (PlayerNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
