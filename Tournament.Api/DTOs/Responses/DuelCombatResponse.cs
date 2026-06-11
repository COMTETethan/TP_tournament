namespace Tournament.Api.DTOs.Responses;

/// <summary>
/// A tournament duel being fought as a combat. Ties the duel to its live combat and, once the
/// fight ends, surfaces the resolved winner and the duel outcome written back to the tournament.
/// <para><paramref name="WinnerPlayerId"/> and <paramref name="DuelOutcome"/> are null until the
/// combat completes.</para>
/// </summary>
public record DuelCombatResponse(
    int DuelId,
    int TournamentId,
    int CombatId,
    string CombatStatus,
    int? WinnerSlot,
    int? WinnerPlayerId,
    string? DuelOutcome,
    CombatResponse Combat);
