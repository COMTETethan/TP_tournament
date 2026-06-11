namespace Tournament.Api.DTOs.Responses;

/// <summary>A single human-readable line describing what happened on a given turn.</summary>
public record CombatLogEntry(int Turn, string Message);
