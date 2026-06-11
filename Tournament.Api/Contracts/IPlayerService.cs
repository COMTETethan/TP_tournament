using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

/// <summary>Champions owned by users. A champion is independent of any tournament.</summary>
public interface IPlayerService
{
    Task<PlayerResponse> CreatePlayerAsync(int userId, CreatePlayerRequest request);
    Task<PlayerResponse> GetPlayerAsync(int id);
    Task<IEnumerable<PlayerResponse>> GetUserPlayersAsync(int userId);
}
