namespace Tournament.Api.DTOs.Responses;

/// <summary>
/// A champion's participation in a tournament. Carries the per-tournament state
/// (disqualification, penalty points) — independent from the champion's other tournaments.
/// </summary>
public record RegistrationResponse(
    int TournamentId,
    int PlayerId,
    string PlayerName,
    int ClassId,
    int Level,
    bool IsDisqualified,
    int PenaltyPoints);
