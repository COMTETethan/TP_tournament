using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/objectives")]
public class ObjectivesController : ControllerBase
{
    private readonly IObjectiveService _objectiveService;

    public ObjectivesController(IObjectiveService objectiveService)
    {
        _objectiveService = objectiveService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ObjectiveResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateObjective([FromBody] CreateObjectiveRequest request)
    {
        try
        {
            var created = await _objectiveService.CreateObjectiveAsync(request);
            return CreatedAtAction(nameof(GetObjective), new { id = created.Id }, created);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ObjectiveResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetObjective(int id)
    {
        try { return Ok(await _objectiveService.GetObjectiveAsync(id)); }
        catch (ObjectiveNotFoundException ex) { return NotFound(ex.Message); }
    }

    [HttpGet("season/{seasonId:int}")]
    [ProducesResponseType(typeof(IEnumerable<ObjectiveResponse>), 200)]
    public async Task<IActionResult> GetSeasonObjectives(int seasonId)
        => Ok(await _objectiveService.GetSeasonObjectivesAsync(seasonId));

    [HttpGet("{id:int}/players/{playerId:int}/progress")]
    [ProducesResponseType(typeof(PlayerObjectiveProgressResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayerProgress(int id, int playerId)
    {
        try { return Ok(await _objectiveService.GetPlayerProgressAsync(id, playerId)); }
        catch (ObjectiveNotFoundException ex) { return NotFound(ex.Message); }
    }

    [HttpPatch("{id:int}/players/{playerId:int}/progress")]
    [ProducesResponseType(typeof(PlayerObjectiveProgressResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdatePlayerProgress(int id, int playerId, [FromBody] UpdateObjectiveProgressRequest request)
    {
        try { return Ok(await _objectiveService.UpdatePlayerProgressAsync(id, playerId, request)); }
        catch (ObjectiveNotFoundException ex) { return NotFound(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }
}
