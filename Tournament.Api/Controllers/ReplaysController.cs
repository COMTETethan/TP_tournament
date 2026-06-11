using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
public class ReplaysController : ControllerBase
{
    private readonly IReplayService _replayService;

    public ReplaysController(IReplayService replayService)
    {
        _replayService = replayService;
    }

    /// <summary>Start recording a replay for a duel.</summary>
    [HttpPost("api/duels/{duelId:int}/replay")]
    [ProducesResponseType(typeof(ReplayResponse), 201)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> StartReplay(int duelId)
    {
        try
        {
            var replay = await _replayService.StartReplayAsync(duelId);
            return CreatedAtAction(nameof(GetReplay), new { duelId }, replay);
        }
        catch (DuelNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Get the replay metadata for a duel.</summary>
    [HttpGet("api/duels/{duelId:int}/replay")]
    [ProducesResponseType(typeof(ReplayResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetReplay(int duelId)
    {
        try
        {
            var replay = await _replayService.GetReplayAsync(duelId);
            return Ok(replay);
        }
        catch (ReplayNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Add an event to the replay stream.</summary>
    [HttpPost("api/duels/{duelId:int}/replay/events")]
    [ProducesResponseType(typeof(ReplayEventResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddEvent(int duelId, [FromBody] AddReplayEventRequest request)
    {
        try
        {
            var evt = await _replayService.AddEventAsync(duelId, request);
            return StatusCode(201, evt);
        }
        catch (ReplayNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>Get all events of a duel's replay in chronological order.</summary>
    [HttpGet("api/duels/{duelId:int}/replay/events")]
    [ProducesResponseType(typeof(IEnumerable<ReplayEventResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetEvents(int duelId)
    {
        try
        {
            var events = await _replayService.GetEventsAsync(duelId);
            return Ok(events);
        }
        catch (ReplayNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Mark a replay as complete.</summary>
    [HttpPatch("api/duels/{duelId:int}/replay/complete")]
    [ProducesResponseType(typeof(ReplayResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CompleteReplay(int duelId)
    {
        try
        {
            var replay = await _replayService.CompleteReplayAsync(duelId);
            return Ok(replay);
        }
        catch (ReplayNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Get the cosmetic snapshot taken at the start of a duel.</summary>
    [HttpGet("api/duels/{duelId:int}/cosmetic-snapshot")]
    [ProducesResponseType(typeof(CosmeticSnapshotResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCosmeticSnapshot(int duelId)
    {
        try
        {
            var snapshot = await _replayService.GetCosmeticSnapshotAsync(duelId);
            return Ok(snapshot);
        }
        catch (DuelNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
