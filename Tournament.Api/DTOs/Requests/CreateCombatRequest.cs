namespace Tournament.Api.DTOs.Requests;

/// <summary>Start a combat between two champions (slot 1 and slot 2).</summary>
public record CreateCombatRequest(CombatantSpec Champion1, CombatantSpec Champion2);
