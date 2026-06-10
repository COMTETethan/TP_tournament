using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

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
        => throw new NotImplementedException();

    /// <summary>List all duels in a tournament.</summary>
    [HttpGet("api/tournaments/{tournamentId:int}/duels")]
    [ProducesResponseType(typeof(IEnumerable<DuelResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTournamentDuels(int tournamentId)
        => throw new NotImplementedException();

    /// <summary>Get a single duel by id.</summary>
    [HttpGet("api/duels/{id:int}")]
    [ProducesResponseType(typeof(DuelResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDuel(int id)
        => throw new NotImplementedException();

    /// <summary>Set the outcome of a duel.</summary>
    [HttpPatch("api/duels/{id:int}/outcome")]
    [ProducesResponseType(typeof(DuelResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SetDuelOutcome(int id, [FromBody] SetDuelOutcomeRequest request)
        => throw new NotImplementedException();

    /// <summary>End a duel and record its duration.</summary>
    [HttpPost("api/duels/{id:int}/end")]
    [ProducesResponseType(typeof(DuelResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> EndDuel(int id, [FromBody] EndDuelRequest request)
        => throw new NotImplementedException();
}
