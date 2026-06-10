namespace Tournament.Api.DTOs.Responses;

public record RankingResponse(int TournamentId, IReadOnlyList<PlayerScoreResponse> Ranking);
