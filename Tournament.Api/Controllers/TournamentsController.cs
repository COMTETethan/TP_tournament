using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/tournaments")]
public class TournamentsController : ControllerBase
{
    private readonly ITournamentService _tournamentService;

    public TournamentsController(ITournamentService tournamentService)
    {
        _tournamentService = tournamentService;
    }

    /// <summary>Create a new tournament.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TournamentResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateTournament([FromBody] CreateTournamentRequest request)
    {
        try
        {
            var created = await _tournamentService.CreateTournamentAsync(request);
            return CreatedAtAction(nameof(GetTournament), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Get a tournament by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TournamentResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTournament(int id)
    {
        try
        {
            var tournament = await _tournamentService.GetTournamentAsync(id);
            return Ok(tournament);
        }
        catch (TournamentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>List all tournaments.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TournamentResponse>), 200)]
    public async Task<IActionResult> GetAllTournaments()
    {
        var list = await _tournamentService.GetAllTournamentsAsync();
        return Ok(list);
    }

    /// <summary>Update tournament status (OPEN → IN_PROGRESS → CLOSED).</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(TournamentResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateTournamentStatus(int id, [FromBody] UpdateTournamentStatusRequest request)
    {
        try
        {
            var updated = await _tournamentService.UpdateTournamentStatusAsync(id, request);
            return Ok(updated);
        }
        catch (InvalidTournamentStatusException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (TournamentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
