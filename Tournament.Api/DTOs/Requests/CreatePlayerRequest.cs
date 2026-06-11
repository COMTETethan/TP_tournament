namespace Tournament.Api.DTOs.Requests;

/// <summary>
/// Add a player to a tournament. Optionally assign a champion class and level so the player can
/// fight (max HP = 100 + 10 × <paramref name="Level"/>). Level defaults to 1.
/// </summary>
public record CreatePlayerRequest(string Name, int? ClassId = null, int Level = 1);
