namespace Tournament.Api.DTOs.Responses;

public record SeasonResponse(int Id, string Name, string Status, DateTime StartDate, DateTime EndDate, DateTime CreatedAt);
