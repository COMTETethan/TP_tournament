namespace Tournament.Api.DTOs.Responses;

/// <summary>
/// A challenge between two users. <paramref name="Status"/> is PENDING, ACCEPTED or DECLINED.
/// <paramref name="CombatId"/> is set once the challenge is accepted (a combat is created).
/// </summary>
public record ChallengeResponse(
    int Id,
    int ChallengerUserId,
    string ChallengerEmail,
    int OpponentUserId,
    string OpponentEmail,
    string Status,
    int? CombatId,
    DateTime CreatedAt
);
