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

    /// <summary>Create a new objective for a season.</summary>
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

    /// <summary>Get an objective by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ObjectiveResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetObjective(int id)
    {
        try { return Ok(await _objectiveService.GetObjectiveAsync(id)); }
        catch (ObjectiveNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>List all objectives for a season.</summary>
    [HttpGet("season/{seasonId:int}")]
    [ProducesResponseType(typeof(IEnumerable<ObjectiveResponse>), 200)]
    public async Task<IActionResult> GetSeasonObjectives(int seasonId)
        => Ok(await _objectiveService.GetSeasonObjectivesAsync(seasonId));

    /// <summary>Get a player's progress on an objective. Pass periodKey for DAILY (YYYY-MM-DD) or WEEKLY (YYYY-WNN) objectives.</summary>
    [HttpGet("{id:int}/players/{playerId:int}/progress")]
    [ProducesResponseType(typeof(PlayerObjectiveProgressResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayerProgress(int id, int playerId, [FromQuery] string? periodKey = null)
    {
        try { return Ok(await _objectiveService.GetPlayerProgressAsync(id, playerId, periodKey)); }
        catch (ObjectiveNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>List all completions for a player on an objective (one entry per period for recurring objectives).</summary>
    [HttpGet("{id:int}/players/{playerId:int}/completions")]
    [ProducesResponseType(typeof(IEnumerable<PlayerObjectiveCompletionResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayerCompletions(int id, int playerId)
    {
        try { return Ok(await _objectiveService.GetPlayerCompletionsAsync(id, playerId)); }
        catch (ObjectiveNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Update a player's progress on an objective.</summary>
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
