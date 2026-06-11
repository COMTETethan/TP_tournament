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
        throw new NotImplementedException();
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ObjectiveResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetObjective(int id)
    {
        throw new NotImplementedException();
    }

    [HttpGet("season/{seasonId:int}")]
    [ProducesResponseType(typeof(IEnumerable<ObjectiveResponse>), 200)]
    public async Task<IActionResult> GetSeasonObjectives(int seasonId)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id:int}/players/{playerId:int}/progress")]
    [ProducesResponseType(typeof(PlayerObjectiveProgressResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPlayerProgress(int id, int playerId)
    {
        throw new NotImplementedException();
    }

    [HttpPatch("{id:int}/players/{playerId:int}/progress")]
    [ProducesResponseType(typeof(PlayerObjectiveProgressResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdatePlayerProgress(int id, int playerId, [FromBody] UpdateObjectiveProgressRequest request)
    {
        throw new NotImplementedException();
    }
}
