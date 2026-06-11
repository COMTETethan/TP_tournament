namespace Tournament.Api.DTOs.Requests;

public record CreateSeasonRequest(string Name, DateTime StartDate, DateTime EndDate);
