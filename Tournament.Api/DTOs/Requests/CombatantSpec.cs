namespace Tournament.Api.DTOs.Requests;

/// <summary>Definition of one champion entering a combat: a display name, a class and a level.</summary>
public record CombatantSpec(string Name, int ClassId, int Level);
