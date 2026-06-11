using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

/// <summary>
/// A champion's participation in a tournament (registration). Holds per-tournament state:
/// disqualification and penalty points. The same champion can be registered in many tournaments.
/// </summary>
public interface ITournamentPlayerService
{
    Task<RegistrationResponse> RegisterAsync(int tournamentId, int playerId);
    Task<IEnumerable<RegistrationResponse>> GetTournamentPlayersAsync(int tournamentId);
    Task<RegistrationResponse> GetRegistrationAsync(int tournamentId, int playerId);
    Task<RegistrationResponse> DisqualifyAsync(int tournamentId, int playerId);
    Task<RegistrationResponse> AddPenaltyAsync(int tournamentId, int playerId, AddPenaltyRequest request);
}
