namespace Tournament.Api.DTOs.Responses;

/// <summary>
/// A class skill. <paramref name="Category"/> is one of ATTACK, DEFEND, HEAL, AURA.
/// <paramref name="Power"/> is the magnitude (damage, shield, healing or effect strength),
/// <paramref name="Duration"/> the number of turns an effect lasts (0 for instant skills),
/// and <paramref name="AuraEffect"/> the buff/debuff applied by AURA skills
/// (ATTACK_UP, ATTACK_DOWN, DEFENSE_UP, DEFENSE_DOWN); null for non-AURA skills.
/// </summary>
public record SkillResponse(
    int Id,
    int ClassId,
    string Name,
    string Category,
    int Power,
    int Duration,
    string? AuraEffect,
    string Description);
