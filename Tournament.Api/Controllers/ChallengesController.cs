using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/challenges")]
public class ChallengesController : ControllerBase
{
    private readonly IChallengeService _challengeService;

    public ChallengesController(IChallengeService challengeService)
        => _challengeService = challengeService;

    /// <summary>Create a challenge from one user to another.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChallengeResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Create([FromBody] CreateChallengeRequest request)
    {
        try
        {
            var challenge = await _challengeService.CreateChallengeAsync(request);
            return StatusCode(201, challenge);
        }
        catch (ArgumentException ex)     { return BadRequest(ex.Message); }
        catch (UserNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Accept a challenge — a combat is created and its id returned.</summary>
    [HttpPost("{id:int}/accept")]
    [ProducesResponseType(typeof(ChallengeResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Accept(int id)
    {
        try { return Ok(await _challengeService.AcceptChallengeAsync(id)); }
        catch (ChallengeNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Decline a challenge.</summary>
    [HttpPost("{id:int}/decline")]
    [ProducesResponseType(typeof(ChallengeResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Decline(int id)
    {
        try { return Ok(await _challengeService.DeclineChallengeAsync(id)); }
        catch (ChallengeNotFoundException ex) { return NotFound(ex.Message); }
    }
}
