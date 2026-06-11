using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/duels/{duelId:int}/combat")]
public class DuelCombatController : ControllerBase
{
    private readonly IDuelCombatService _service;

    public DuelCombatController(IDuelCombatService service)
    {
        _service = service;
    }

    /// <summary>Start a combat for a duel; its two players fight as champions.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(DuelCombatResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> StartCombatForDuel(int duelId)
    {
        try
        {
            var created = await _service.StartFromDuelAsync(duelId);
            return CreatedAtAction(nameof(GetDuelCombat), new { duelId }, created);
        }
        catch (DuelNotFoundException ex)        { return NotFound(ex.Message); }
        catch (PlayerNotFoundException ex)      { return NotFound(ex.Message); }
        catch (ClassNotFoundException ex)       { return NotFound(ex.Message); }
        catch (InvalidCombatActionException ex) { return BadRequest(ex.Message); }
        catch (ArgumentException ex)            { return BadRequest(ex.Message); }
    }

    /// <summary>Get the current state of a duel's combat.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(DuelCombatResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDuelCombat(int duelId)
    {
        try { return Ok(await _service.GetByDuelAsync(duelId)); }
        catch (DuelNotFoundException ex)        { return NotFound(ex.Message); }
        catch (InvalidCombatActionException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>
    /// Submit a champion's skill for the current turn. When the combat ends, the duel outcome
    /// (and thus the tournament score) is written automatically.
    /// </summary>
    [HttpPost("actions")]
    [ProducesResponseType(typeof(DuelCombatResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SubmitAction(int duelId, [FromBody] SubmitActionRequest request)
    {
        try { return Ok(await _service.SubmitActionAsync(duelId, request)); }
        catch (DuelNotFoundException ex)        { return NotFound(ex.Message); }
        catch (CombatNotFoundException ex)      { return NotFound(ex.Message); }
        catch (SkillNotFoundException ex)       { return NotFound(ex.Message); }
        catch (InvalidCombatActionException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>The given champion concedes; the duel outcome is written for the opponent.</summary>
    [HttpPost("forfeit")]
    [ProducesResponseType(typeof(DuelCombatResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Forfeit(int duelId, [FromBody] ForfeitRequest request)
    {
        try { return Ok(await _service.ForfeitAsync(duelId, request)); }
        catch (DuelNotFoundException ex)        { return NotFound(ex.Message); }
        catch (InvalidCombatActionException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>Get the recorded, read-only replay of a duel's combat.</summary>
    [HttpGet("replay")]
    [ProducesResponseType(typeof(CombatReplayResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetReplay(int duelId)
    {
        try { return Ok(await _service.GetReplayByDuelAsync(duelId)); }
        catch (DuelNotFoundException ex)        { return NotFound(ex.Message); }
        catch (CombatNotFoundException ex)      { return NotFound(ex.Message); }
        catch (InvalidCombatActionException ex) { return BadRequest(ex.Message); }
    }
}
