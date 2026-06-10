using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

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
        => throw new NotImplementedException();

    /// <summary>List all players in a tournament.</summary>
    [HttpGet("api/tournaments/{tournamentId:int}/players")]
    [ProducesResponseType(typeof(IEnumerable<PlayerResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTournamentPlayers(int tournamentId)
        => throw new NotImplementedException();

    /// <summary>Get a player by id.</summary>
    [HttpGet("api/players/{id:int}")]
    [ProducesResponseType(typeof(PlayerResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayer(int id)
        => throw new NotImplementedException();

    /// <summary>Disqualify a player (score reset to 0).</summary>
    [HttpPost("api/players/{id:int}/disqualify")]
    [ProducesResponseType(typeof(PlayerResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DisqualifyPlayer(int id)
        => throw new NotImplementedException();

    /// <summary>Add penalty points to a player.</summary>
    [HttpPatch("api/players/{id:int}/penalties")]
    [ProducesResponseType(typeof(PlayerResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddPenalty(int id, [FromBody] AddPenaltyRequest request)
        => throw new NotImplementedException();
}
