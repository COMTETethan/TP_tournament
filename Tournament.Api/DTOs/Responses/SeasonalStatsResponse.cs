namespace Tournament.Api.DTOs.Responses;

public record SeasonalStatsResponse(int PlayerId, int SeasonId, int TotalScore, int TournamentsPlayed, int TotalWins, int TotalLosses, int TotalDraws, int WinStreakBest, int? SeasonRank);
