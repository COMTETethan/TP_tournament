using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
public class DuelsController : ControllerBase
{
    private readonly IDuelService _duelService;

    public DuelsController(IDuelService duelService)
    {
        _duelService = duelService;
    }

    /// <summary>Create a duel between two players in a tournament.</summary>
    [HttpPost("api/tournaments/{tournamentId:int}/duels")]
    [ProducesResponseType(typeof(DuelResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateDuel(int tournamentId, [FromBody] CreateDuelRequest request)
    {
        try
        {
            var created = await _duelService.CreateDuelAsync(tournamentId, request);
            return CreatedAtAction(nameof(GetDuel), new { id = created.Id }, created);
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

    /// <summary>List all duels in a tournament.</summary>
    [HttpGet("api/tournaments/{tournamentId:int}/duels")]
    [ProducesResponseType(typeof(IEnumerable<DuelResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTournamentDuels(int tournamentId)
    {
        try
        {
            var list = await _duelService.GetTournamentDuelsAsync(tournamentId);
            return Ok(list);
        }
        catch (TournamentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Get a single duel by id.</summary>
    [HttpGet("api/duels/{id:int}")]
    [ProducesResponseType(typeof(DuelResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDuel(int id)
    {
        try
        {
            var duel = await _duelService.GetDuelAsync(id);
            return Ok(duel);
        }
        catch (DuelNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Set the outcome of a duel.</summary>
    [HttpPatch("api/duels/{id:int}/outcome")]
    [ProducesResponseType(typeof(DuelResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SetDuelOutcome(int id, [FromBody] SetDuelOutcomeRequest request)
    {
        try
        {
            var updated = await _duelService.SetDuelOutcomeAsync(id, request);
            return Ok(updated);
        }
        catch (DuelNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>End a duel and record its duration.</summary>
    [HttpPost("api/duels/{id:int}/end")]
    [ProducesResponseType(typeof(DuelResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> EndDuel(int id, [FromBody] EndDuelRequest request)
    {
        try
        {
            var ended = await _duelService.EndDuelAsync(id, request);
            return Ok(ended);
        }
        catch (DuelNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (DuelAlreadyEndedException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
