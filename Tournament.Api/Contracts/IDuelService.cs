using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface IDuelService
{
    Task<DuelResponse> CreateDuelAsync(int tournamentId, CreateDuelRequest request);
    Task<DuelResponse> GetDuelAsync(int id);
    Task<IEnumerable<DuelResponse>> GetTournamentDuelsAsync(int tournamentId);
    Task<DuelResponse> SetDuelOutcomeAsync(int id, SetDuelOutcomeRequest request);
    Task<DuelResponse> EndDuelAsync(int id, EndDuelRequest request);
}
