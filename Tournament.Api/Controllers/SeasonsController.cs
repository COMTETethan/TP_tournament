using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/seasons")]
public class SeasonsController : ControllerBase
{
    private readonly ISeasonService _seasonService;

    public SeasonsController(ISeasonService seasonService)
    {
        _seasonService = seasonService;
    }

    /// <summary>Create a new season.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SeasonResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateSeason([FromBody] CreateSeasonRequest request)
    {
        try
        {
            var created = await _seasonService.CreateSeasonAsync(request);
            return CreatedAtAction(nameof(GetSeason), new { id = created.Id }, created);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>Get a season by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SeasonResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSeason(int id)
    {
        try { return Ok(await _seasonService.GetSeasonAsync(id)); }
        catch (SeasonNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>List all seasons.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SeasonResponse>), 200)]
    public async Task<IActionResult> GetAllSeasons()
        => Ok(await _seasonService.GetAllSeasonsAsync());

    /// <summary>Update season status (UPCOMING → ACTIVE → ENDED).</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(SeasonResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateSeasonStatus(int id, [FromBody] UpdateSeasonStatusRequest request)
    {
        try { return Ok(await _seasonService.UpdateSeasonStatusAsync(id, request)); }
        catch (InvalidSeasonStatusException ex) { return BadRequest(ex.Message); }
        catch (SeasonNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Associate a tournament with a season.</summary>
    [HttpPost("{id:int}/tournaments/{tournamentId:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddTournamentToSeason(int id, int tournamentId)
    {
        try
        {
            await _seasonService.AddTournamentToSeasonAsync(id, tournamentId);
            return NoContent();
        }
        catch (SeasonNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Get a player's seasonal stats (score, wins, losses, draws).</summary>
    [HttpGet("{id:int}/players/{playerId:int}/stats")]
    [ProducesResponseType(typeof(SeasonalStatsResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayerSeasonalStats(int id, int playerId)
    {
        try { return Ok(await _seasonService.GetPlayerSeasonalStatsAsync(id, playerId)); }
        catch (SeasonNotFoundException ex) { return NotFound(ex.Message); }
    }
}
