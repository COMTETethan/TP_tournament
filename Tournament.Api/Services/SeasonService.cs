using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Services;

public class SeasonService : ISeasonService
{
    public Task<SeasonResponse> CreateSeasonAsync(CreateSeasonRequest request)
        => throw new NotImplementedException();

    public Task<SeasonResponse> GetSeasonAsync(int id)
        => throw new NotImplementedException();

    public Task<IEnumerable<SeasonResponse>> GetAllSeasonsAsync()
        => throw new NotImplementedException();

    public Task<SeasonResponse> UpdateSeasonStatusAsync(int id, UpdateSeasonStatusRequest request)
        => throw new NotImplementedException();

    public Task AddTournamentToSeasonAsync(int seasonId, int tournamentId)
        => throw new NotImplementedException();

    public Task<SeasonalStatsResponse> GetPlayerSeasonalStatsAsync(int seasonId, int playerId)
        => throw new NotImplementedException();
}
