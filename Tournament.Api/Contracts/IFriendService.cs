using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface IFriendService
{
    Task<IEnumerable<UserSummaryResponse>> GetAllUsersAsync();
    Task<IEnumerable<UserSummaryResponse>> GetFriendsAsync(int userId);
    Task AddFriendAsync(int userId, int friendUserId);
    Task RemoveFriendAsync(int userId, int friendUserId);
}
