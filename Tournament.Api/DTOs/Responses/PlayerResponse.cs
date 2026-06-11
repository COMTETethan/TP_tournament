namespace Tournament.Api.DTOs.Responses;

/// <summary>
/// A tournament player. When <paramref name="ClassId"/> is set the player is a fightable champion
/// (max HP = 100 + 10 × <paramref name="Level"/>); a player with no class cannot enter a combat.
/// </summary>
public record PlayerResponse(
    int Id,
    int TournamentId,
    string Name,
    bool IsDisqualified,
    int PenaltyPoints,
    int? ClassId = null,
    int Level = 1);
