using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface ISeasonService
{
    Task<SeasonResponse> CreateSeasonAsync(CreateSeasonRequest request);
    Task<SeasonResponse> GetSeasonAsync(int id);
    Task<IEnumerable<SeasonResponse>> GetAllSeasonsAsync();
    Task<SeasonResponse> UpdateSeasonStatusAsync(int id, UpdateSeasonStatusRequest request);
    Task AddTournamentToSeasonAsync(int seasonId, int tournamentId);
    Task<SeasonalStatsResponse> GetPlayerSeasonalStatsAsync(int seasonId, int playerId);
}
