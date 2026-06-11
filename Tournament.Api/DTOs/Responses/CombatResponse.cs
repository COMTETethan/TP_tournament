namespace Tournament.Api.DTOs.Responses;

/// <summary>
/// The full state of a combat. <paramref name="Status"/> is IN_PROGRESS or COMPLETED.
/// <paramref name="WinnerSlot"/> is null until the combat ends.
/// </summary>
public record CombatResponse(
    int Id,
    string Status,
    int Turn,
    int? WinnerSlot,
    CombatantState Champion1,
    CombatantState Champion2,
    IReadOnlyList<CombatLogEntry> Log,
    DateTime CreatedAt);
