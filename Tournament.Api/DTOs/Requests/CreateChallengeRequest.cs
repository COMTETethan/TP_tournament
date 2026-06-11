namespace Tournament.Api.DTOs.Requests;

public record CreateChallengeRequest(int ChallengerUserId, int OpponentUserId);
