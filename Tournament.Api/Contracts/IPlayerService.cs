using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface IPlayerService
{
    Task<PlayerResponse> AddPlayerAsync(int tournamentId, CreatePlayerRequest request);
    Task<PlayerResponse> GetPlayerAsync(int id);
    Task<IEnumerable<PlayerResponse>> GetTournamentPlayersAsync(int tournamentId);
    Task<PlayerResponse> DisqualifyPlayerAsync(int id);
    Task<PlayerResponse> AddPenaltyAsync(int id, AddPenaltyRequest request);
}
