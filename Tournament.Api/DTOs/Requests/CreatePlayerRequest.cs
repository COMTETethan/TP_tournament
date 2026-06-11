namespace Tournament.Api.DTOs.Requests;

/// <summary>
/// Create a champion owned by the current user. A champion has a class and a level
/// (max HP = 100 + 10 × <paramref name="Level"/>) and can be entered into several tournaments.
/// </summary>
public record CreatePlayerRequest(string Name, int ClassId, int Level = 1);
