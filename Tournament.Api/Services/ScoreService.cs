using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Services;

public class ScoreService : IScoreService
{
    public Task<PlayerScoreResponse> GetPlayerScoreAsync(int playerId)
        => throw new NotImplementedException();

    public Task<RankingResponse> GetTournamentRankingAsync(int tournamentId)
        => throw new NotImplementedException();

    public Task<PlayerScoreResponse> GetTournamentChampionAsync(int tournamentId)
        => throw new NotImplementedException();
}
