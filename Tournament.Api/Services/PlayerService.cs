using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Services;

public class PlayerService : IPlayerService
{
    public Task<PlayerResponse> AddPlayerAsync(int tournamentId, CreatePlayerRequest request)
        => throw new NotImplementedException();

    public Task<PlayerResponse> GetPlayerAsync(int id)
        => throw new NotImplementedException();

    public Task<IEnumerable<PlayerResponse>> GetTournamentPlayersAsync(int tournamentId)
        => throw new NotImplementedException();

    public Task<PlayerResponse> DisqualifyPlayerAsync(int id)
        => throw new NotImplementedException();

    public Task<PlayerResponse> AddPenaltyAsync(int id, AddPenaltyRequest request)
        => throw new NotImplementedException();
}
