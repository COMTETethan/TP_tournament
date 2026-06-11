namespace Tournament.Api.DTOs.Responses;

/// <summary>
/// The recorded replay of a combat: champion summaries plus the full ordered event stream.
/// <para><paramref name="Status"/> is IN_PROGRESS or COMPLETED; <paramref name="CompletedAt"/> is
/// null while the fight is ongoing. The frontend consumes this read-only to watch an old combat.</para>
/// </summary>
public record CombatReplayResponse(
    int CombatId,
    string Status,
    int? WinnerSlot,
    int TurnCount,
    ReplayChampion Champion1,
    ReplayChampion Champion2,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    IReadOnlyList<CombatEventResponse> Events);
