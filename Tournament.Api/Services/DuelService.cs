using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Services;

public class DuelService : IDuelService
{
    public Task<DuelResponse> CreateDuelAsync(int tournamentId, CreateDuelRequest request)
        => throw new NotImplementedException();

    public Task<DuelResponse> GetDuelAsync(int id)
        => throw new NotImplementedException();

    public Task<IEnumerable<DuelResponse>> GetTournamentDuelsAsync(int tournamentId)
        => throw new NotImplementedException();

    public Task<DuelResponse> SetDuelOutcomeAsync(int id, SetDuelOutcomeRequest request)
        => throw new NotImplementedException();

    public Task<DuelResponse> EndDuelAsync(int id, EndDuelRequest request)
        => throw new NotImplementedException();
}
