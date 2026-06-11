namespace Tournament.Api.DTOs.Responses;

/// <summary>Summary of a champion as it appears in a recorded replay.</summary>
public record ReplayChampion(int Slot, string Name, int ClassId, string ClassName, int Level, int MaxHp);
