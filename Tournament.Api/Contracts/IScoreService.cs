using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface IScoreService
{
    Task<PlayerScoreResponse> GetPlayerScoreAsync(int playerId);
    Task<RankingResponse> GetTournamentRankingAsync(int tournamentId);
    Task<PlayerScoreResponse> GetTournamentChampionAsync(int tournamentId);
}
