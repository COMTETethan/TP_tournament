namespace Tournament.Api.DTOs.Responses;

public record TournamentResponse(int Id, string Name, string Status, DateTime CreatedAt);
