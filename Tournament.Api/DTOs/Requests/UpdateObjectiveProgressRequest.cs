namespace Tournament.Api.DTOs.Requests;

public record UpdateObjectiveProgressRequest(int NewValue, string? PeriodKey = null);
