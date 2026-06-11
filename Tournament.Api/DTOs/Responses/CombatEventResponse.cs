namespace Tournament.Api.DTOs.Responses;

/// <summary>
/// One recorded step of a combat, ordered by <paramref name="Sequence"/>. Carries a snapshot of
/// both champions' HP after the step so a frontend can faithfully animate the replay.
/// <para>
/// <paramref name="Type"/>: COMBAT_START, ATTACK, DEFEND, HEAL, AURA, SKIPPED, VICTORY, FORFEIT.
/// <paramref name="Amount"/> is the damage dealt, HP healed, shield gained or effect magnitude.
/// <paramref name="Effect"/> is set for AURA steps (e.g. ATTACK_UP).
/// </para>
/// </summary>
public record CombatEventResponse(
    int Sequence,
    int Turn,
    string Type,
    int? ActorSlot,
    int? SkillId,
    string? SkillName,
    string? Category,
    int? TargetSlot,
    int? Amount,
    string? Effect,
    int Champion1Hp,
    int Champion2Hp,
    string Message);
