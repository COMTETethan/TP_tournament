namespace Tournament.Api.DTOs.Responses;

public record PlayerScoreResponse(int PlayerId, string PlayerName, int FinalScore, bool IsDisqualified);
