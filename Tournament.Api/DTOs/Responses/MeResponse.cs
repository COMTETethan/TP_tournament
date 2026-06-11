namespace Tournament.Api.DTOs.Responses;

public record MeResponse(int Id, string Email, int? PlayerId, DateTime CreatedAt);
