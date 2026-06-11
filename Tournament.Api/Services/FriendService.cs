using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

/// <summary>
/// In-memory friendships between registered users. The store is static so it
/// persists across requests regardless of the service lifetime.
/// </summary>
public class FriendService : IFriendService
{
    private static readonly object Lock = new();
    // userId -> set of friend ids (kept bidirectional)
    private static readonly Dictionary<int, HashSet<int>> Friendships = new();

    public Task<IEnumerable<UserSummaryResponse>> GetAllUsersAsync()
        => Task.FromResult(AuthService.AllUsers()
            .Select(u => new UserSummaryResponse(u.Id, u.Email)));

    public Task<IEnumerable<UserSummaryResponse>> GetFriendsAsync(int userId)
    {
        if (!AuthService.UserExists(userId))
            throw new UserNotFoundException(userId);

        lock (Lock)
        {
            var ids = Friendships.TryGetValue(userId, out var set) ? set : new HashSet<int>();
            var friends = ids
                .Select(id => new UserSummaryResponse(id, AuthService.EmailOf(id) ?? ""))
                .ToList();
            return Task.FromResult<IEnumerable<UserSummaryResponse>>(friends);
        }
    }

    public Task AddFriendAsync(int userId, int friendUserId)
    {
        if (userId == friendUserId)
            throw new ArgumentException("You cannot add yourself as a friend.");
        if (!AuthService.UserExists(userId))
            throw new UserNotFoundException(userId);
        if (!AuthService.UserExists(friendUserId))
            throw new UserNotFoundException(friendUserId);

        lock (Lock)
        {
            Link(userId, friendUserId);
            Link(friendUserId, userId);
        }
        return Task.CompletedTask;
    }

    public Task RemoveFriendAsync(int userId, int friendUserId)
    {
        lock (Lock)
        {
            if (Friendships.TryGetValue(userId, out var a)) a.Remove(friendUserId);
            if (Friendships.TryGetValue(friendUserId, out var b)) b.Remove(userId);
        }
        return Task.CompletedTask;
    }

    private static void Link(int a, int b)
    {
        if (!Friendships.TryGetValue(a, out var set))
        {
            set = new HashSet<int>();
            Friendships[a] = set;
        }
        set.Add(b);
    }
}
