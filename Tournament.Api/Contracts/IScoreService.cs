using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface IScoreService
{
    /// <summary>Score of a champion within a specific tournament (derived from that tournament's duels).</summary>
    Task<PlayerScoreResponse> GetPlayerScoreAsync(int tournamentId, int playerId);
    Task<RankingResponse> GetTournamentRankingAsync(int tournamentId);
    Task<PlayerScoreResponse> GetTournamentChampionAsync(int tournamentId);
}
