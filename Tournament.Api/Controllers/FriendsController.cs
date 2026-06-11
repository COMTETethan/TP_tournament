using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Controllers;

[ApiController]
public class FriendsController : ControllerBase
{
    private readonly IFriendService _friendService;
    private readonly IChallengeService _challengeService;

    public FriendsController(IFriendService friendService, IChallengeService challengeService)
    {
        _friendService = friendService;
        _challengeService = challengeService;
    }

    /// <summary>List all registered users (to find people to add as friends).</summary>
    [HttpGet("api/users")]
    [ProducesResponseType(typeof(IEnumerable<UserSummaryResponse>), 200)]
    public async Task<IActionResult> GetAllUsers()
        => Ok(await _friendService.GetAllUsersAsync());

    /// <summary>List a user's friends.</summary>
    [HttpGet("api/users/{userId:int}/friends")]
    [ProducesResponseType(typeof(IEnumerable<UserSummaryResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetFriends(int userId)
    {
        try { return Ok(await _friendService.GetFriendsAsync(userId)); }
        catch (UserNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Add a friend (bidirectional).</summary>
    [HttpPost("api/users/{userId:int}/friends")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddFriend(int userId, [FromBody] AddFriendRequest request)
    {
        try
        {
            await _friendService.AddFriendAsync(userId, request.FriendUserId);
            return NoContent();
        }
        catch (ArgumentException ex)     { return BadRequest(ex.Message); }
        catch (UserNotFoundException ex) { return NotFound(ex.Message); }
    }

    /// <summary>Remove a friend.</summary>
    [HttpDelete("api/users/{userId:int}/friends/{friendUserId:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> RemoveFriend(int userId, int friendUserId)
    {
        await _friendService.RemoveFriendAsync(userId, friendUserId);
        return NoContent();
    }

    /// <summary>List the challenges a user is involved in (sent or received).</summary>
    [HttpGet("api/users/{userId:int}/challenges")]
    [ProducesResponseType(typeof(IEnumerable<ChallengeResponse>), 200)]
    public async Task<IActionResult> GetUserChallenges(int userId)
        => Ok(await _challengeService.GetUserChallengesAsync(userId));
}
