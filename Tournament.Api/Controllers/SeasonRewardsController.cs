using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/seasons/{seasonId:int}/rewards")]
public class SeasonRewardsController : ControllerBase
{
    private readonly ISeasonRewardService _service;

    public SeasonRewardsController(ISeasonRewardService service)
    {
        _service = service;
    }

    /// <summary>Create a reward tier for a season (rank range → reward).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SeasonRewardResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CreateSeasonReward(int seasonId, [FromBody] CreateSeasonRewardRequest request)
    {
        try
        {
            var created = await _service.CreateSeasonRewardAsync(seasonId, request);
            return CreatedAtAction(nameof(GetSeasonRewards), new { seasonId }, created);
        }
        catch (SeasonNotFoundException ex) { return NotFound(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>List all reward tiers for a season.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SeasonRewardResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSeasonRewards(int seasonId)
    {
        try { return Ok(await _service.GetSeasonRewardsAsync(seasonId)); }
        catch (SeasonNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Distribute season rewards to players based on their final rank (idempotent).</summary>
    [HttpPost("distribute")]
    [ProducesResponseType(typeof(IEnumerable<PlayerSeasonRewardResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DistributeRewards(int seasonId)
    {
        try { return Ok(await _service.DistributeRewardsAsync(seasonId)); }
        catch (SeasonNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Get all rewards earned by a player in a season.</summary>
    [HttpGet("players/{playerId:int}")]
    [ProducesResponseType(typeof(IEnumerable<PlayerSeasonRewardResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayerSeasonRewards(int seasonId, int playerId)
    {
        try { return Ok(await _service.GetPlayerSeasonRewardsAsync(seasonId, playerId)); }
        catch (SeasonNotFoundException ex) { return NotFound(ex.Message); }
    }
}
