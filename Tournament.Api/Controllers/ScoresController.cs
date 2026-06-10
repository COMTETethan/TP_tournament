using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Controllers;

[ApiController]
public class ScoresController : ControllerBase
{
    private readonly IScoreService _scoreService;

    public ScoresController(IScoreService scoreService)
    {
        _scoreService = scoreService;
    }

    /// <summary>Get the calculated score of a specific player.</summary>
    [HttpGet("api/players/{playerId:int}/score")]
    [ProducesResponseType(typeof(PlayerScoreResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayerScore(int playerId)
        => throw new NotImplementedException();

    /// <summary>Get the full ranking of a tournament, sorted by score descending.</summary>
    [HttpGet("api/tournaments/{tournamentId:int}/ranking")]
    [ProducesResponseType(typeof(RankingResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTournamentRanking(int tournamentId)
        => throw new NotImplementedException();

    /// <summary>Get the champion (highest score) of a tournament.</summary>
    [HttpGet("api/tournaments/{tournamentId:int}/champion")]
    [ProducesResponseType(typeof(PlayerScoreResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTournamentChampion(int tournamentId)
        => throw new NotImplementedException();
}
