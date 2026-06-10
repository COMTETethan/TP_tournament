using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

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
        => throw new NotImplementedException();

    /// <summary>Get a tournament by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TournamentResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTournament(int id)
        => throw new NotImplementedException();

    /// <summary>List all tournaments.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TournamentResponse>), 200)]
    public async Task<IActionResult> GetAllTournaments()
        => throw new NotImplementedException();

    /// <summary>Update tournament status (OPEN → IN_PROGRESS → CLOSED).</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(TournamentResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateTournamentStatus(int id, [FromBody] UpdateTournamentStatusRequest request)
        => throw new NotImplementedException();
}
