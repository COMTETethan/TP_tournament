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

    [HttpPost]
    [ProducesResponseType(typeof(BattlepassResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateBattlepass([FromBody] CreateBattlepassRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpGet("season/{seasonId:int}")]
    [ProducesResponseType(typeof(BattlepassResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBattlepassBySeason(int seasonId)
    {
        throw new NotImplementedException();
    }

    [HttpPost("{id:int}/tiers")]
    [ProducesResponseType(typeof(BattlepassTierResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddTier(int id, [FromBody] AddBattlepassTierRequest request)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id:int}/tiers")]
    [ProducesResponseType(typeof(IEnumerable<BattlepassTierResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTiers(int id)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id:int}/players/{playerId:int}/progress")]
    [ProducesResponseType(typeof(PlayerBattlepassProgressResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayerProgress(int id, int playerId)
    {
        throw new NotImplementedException();
    }

    [HttpPost("{id:int}/players/{playerId:int}/xp")]
    [ProducesResponseType(typeof(PlayerBattlepassProgressResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddXp(int id, int playerId, [FromBody] AddXpRequest request)
    {
        throw new NotImplementedException();
    }
}
