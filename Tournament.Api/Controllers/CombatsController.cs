using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/combats")]
public class CombatsController : ControllerBase
{
    private readonly ICombatService _combatService;

    public CombatsController(ICombatService combatService)
    {
        _combatService = combatService;
    }

    /// <summary>Start a combat between two champions.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CombatResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> StartCombat([FromBody] CreateCombatRequest request)
    {
        try
        {
            var created = await _combatService.StartCombatAsync(request);
            return CreatedAtAction(nameof(GetCombat), new { id = created.Id }, created);
        }
        catch (ClassNotFoundException ex) { return NotFound(ex.Message); }
        catch (ArgumentException ex)      { return BadRequest(ex.Message); }
    }

    /// <summary>List all combats.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CombatResponse>), 200)]
    public async Task<IActionResult> GetAllCombats()
        => Ok(await _combatService.GetAllCombatsAsync());

    /// <summary>Get the current state of a combat.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CombatResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCombat(int id)
    {
        try { return Ok(await _combatService.GetCombatAsync(id)); }
        catch (CombatNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>
    /// Submit the skill a champion (slot 1 or 2) will use this turn. The turn is resolved
    /// automatically once both champions have submitted their action.
    /// </summary>
    [HttpPost("{id:int}/actions")]
    [ProducesResponseType(typeof(CombatResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SubmitAction(int id, [FromBody] SubmitActionRequest request)
    {
        try { return Ok(await _combatService.SubmitActionAsync(id, request)); }
        catch (CombatNotFoundException ex)      { return NotFound(ex.Message); }
        catch (SkillNotFoundException ex)       { return NotFound(ex.Message); }
        catch (InvalidCombatActionException ex) { return BadRequest(ex.Message); }
    }

    /// <summary>The champion at the given slot concedes; the opponent wins.</summary>
    [HttpPost("{id:int}/forfeit")]
    [ProducesResponseType(typeof(CombatResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Forfeit(int id, [FromBody] ForfeitRequest request)
    {
        try { return Ok(await _combatService.ForfeitAsync(id, request)); }
        catch (CombatNotFoundException ex)      { return NotFound(ex.Message); }
        catch (InvalidCombatActionException ex) { return BadRequest(ex.Message); }
    }
}
