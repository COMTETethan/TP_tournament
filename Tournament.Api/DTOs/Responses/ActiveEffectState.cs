namespace Tournament.Api.DTOs.Responses;

/// <summary>An active buff/debuff on a champion during combat.</summary>
public record ActiveEffectState(string EffectType, int Magnitude, int RemainingTurns);
