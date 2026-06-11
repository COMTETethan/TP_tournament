using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
public class ScoresController : ControllerBase
{
    private readonly IScoreService _scoreService;

    public ScoresController(IScoreService scoreService)
    {
        _scoreService = scoreService;
    }

    /// <summary>Get a champion's score within a tournament.</summary>
    [HttpGet("api/tournaments/{tournamentId:int}/players/{playerId:int}/score")]
    [ProducesResponseType(typeof(PlayerScoreResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayerScore(int tournamentId, int playerId)
    {
        try
        {
            var score = await _scoreService.GetPlayerScoreAsync(tournamentId, playerId);
            return Ok(score);
        }
        catch (RegistrationNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Get the full ranking of a tournament, sorted by score descending.</summary>
    [HttpGet("api/tournaments/{tournamentId:int}/ranking")]
    [ProducesResponseType(typeof(RankingResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTournamentRanking(int tournamentId)
    {
        try
        {
            var ranking = await _scoreService.GetTournamentRankingAsync(tournamentId);
            return Ok(ranking);
        }
        catch (TournamentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Get the champion (highest score) of a tournament.</summary>
    [HttpGet("api/tournaments/{tournamentId:int}/champion")]
    [ProducesResponseType(typeof(PlayerScoreResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTournamentChampion(int tournamentId)
    {
        try
        {
            var champion = await _scoreService.GetTournamentChampionAsync(tournamentId);
            return Ok(champion);
        }
        catch (TournamentNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
