using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/tournaments/{tournamentId:int}/players")]
public class TournamentPlayersController : ControllerBase
{
    private readonly ITournamentPlayerService _service;

    public TournamentPlayersController(ITournamentPlayerService service)
    {
        _service = service;
    }

    /// <summary>Register a champion into a tournament.</summary>
    [HttpPost("{playerId:int}")]
    [ProducesResponseType(typeof(RegistrationResponse), 201)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register(int tournamentId, int playerId)
    {
        try
        {
            var registration = await _service.RegisterAsync(tournamentId, playerId);
            return CreatedAtAction(nameof(GetTournamentPlayers), new { tournamentId }, registration);
        }
        catch (TournamentNotFoundException ex)     { return NotFound(ex.Message); }
        catch (PlayerNotFoundException ex)         { return NotFound(ex.Message); }
        catch (PlayerAlreadyRegisteredException ex) { return Conflict(ex.Message); }
    }

    /// <summary>List the champions registered in a tournament (with per-tournament state).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RegistrationResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTournamentPlayers(int tournamentId)
    {
        try { return Ok(await _service.GetTournamentPlayersAsync(tournamentId)); }
        catch (TournamentNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Disqualify a champion within this tournament (score reset to 0 here only).</summary>
    [HttpPost("{playerId:int}/disqualify")]
    [ProducesResponseType(typeof(RegistrationResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Disqualify(int tournamentId, int playerId)
    {
        try { return Ok(await _service.DisqualifyAsync(tournamentId, playerId)); }
        catch (RegistrationNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Add penalty points to a champion within this tournament.</summary>
    [HttpPatch("{playerId:int}/penalties")]
    [ProducesResponseType(typeof(RegistrationResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddPenalty(int tournamentId, int playerId, [FromBody] AddPenaltyRequest request)
    {
        try { return Ok(await _service.AddPenaltyAsync(tournamentId, playerId, request)); }
        catch (RegistrationNotFoundException ex) { return NotFound(ex.Message); }
        catch (ArgumentException ex)             { return BadRequest(ex.Message); }
    }
}
