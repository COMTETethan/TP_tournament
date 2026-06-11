namespace Tournament.Api.DTOs.Responses;

/// <summary>The live state of one champion in a combat.</summary>
public record CombatantState(
    int Slot,
    string Name,
    int ClassId,
    int Level,
    int MaxHp,
    int CurrentHp,
    bool HasSubmittedAction,
    IReadOnlyList<ActiveEffectState> Effects);
