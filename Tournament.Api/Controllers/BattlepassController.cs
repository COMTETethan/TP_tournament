using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/battlepasses")]
public class BattlepassController : ControllerBase
{
    private readonly IBattlepassService _battlepassService;

    public BattlepassController(IBattlepassService battlepassService)
    {
        _battlepassService = battlepassService;
    }

    /// <summary>Create a battlepass for a season.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BattlepassResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateBattlepass([FromBody] CreateBattlepassRequest request)
    {
        try
        {
            var created = await _battlepassService.CreateBattlepassAsync(request);
            return CreatedAtAction(nameof(GetBattlepassBySeason), new { seasonId = created.SeasonId }, created);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>Get the battlepass for a season.</summary>
    [HttpGet("season/{seasonId:int}")]
    [ProducesResponseType(typeof(BattlepassResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBattlepassBySeason(int seasonId)
    {
        try { return Ok(await _battlepassService.GetBattlepassBySeasonAsync(seasonId)); }
        catch (BattlepassNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Add a tier to a battlepass.</summary>
    [HttpPost("{id:int}/tiers")]
    [ProducesResponseType(typeof(BattlepassTierResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddTier(int id, [FromBody] AddBattlepassTierRequest request)
    {
        try
        {
            var tier = await _battlepassService.AddTierAsync(id, request);
            return CreatedAtAction(nameof(GetTiers), new { id }, tier);
        }
        catch (BattlepassNotFoundException ex) { return NotFound(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>List all tiers of a battlepass.</summary>
    [HttpGet("{id:int}/tiers")]
    [ProducesResponseType(typeof(IEnumerable<BattlepassTierResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTiers(int id)
    {
        try { return Ok(await _battlepassService.GetTiersAsync(id)); }
        catch (BattlepassNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Get a player's battlepass progress.</summary>
    [HttpGet("{id:int}/players/{playerId:int}/progress")]
    [ProducesResponseType(typeof(PlayerBattlepassProgressResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayerProgress(int id, int playerId)
    {
        try { return Ok(await _battlepassService.GetPlayerProgressAsync(id, playerId)); }
        catch (BattlepassNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Add XP to a player's battlepass, unlocking tiers if thresholds are reached.</summary>
    [HttpPost("{id:int}/players/{playerId:int}/xp")]
    [ProducesResponseType(typeof(PlayerBattlepassProgressResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddXp(int id, int playerId, [FromBody] AddXpRequest request)
    {
        try { return Ok(await _battlepassService.AddXpAsync(id, playerId, request)); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (BattlepassNotFoundException ex) { return NotFound(ex.Message); }
    }
}
