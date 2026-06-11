namespace Tournament.Api.DTOs.Responses;

/// <summary>
/// A champion owned by a user (<paramref name="UserId"/>), defined by a class and a level
/// (max HP = 100 + 10 × <paramref name="Level"/>). The same champion can be registered in
/// several tournaments; per-tournament state lives on the registration, not here.
/// </summary>
public record PlayerResponse(int Id, int UserId, string Name, int ClassId, int Level);
