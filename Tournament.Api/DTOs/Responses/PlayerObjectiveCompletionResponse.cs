namespace Tournament.Api.DTOs.Responses;

public record PlayerObjectiveCompletionResponse(
    int Id,
    int PlayerId,
    int ObjectiveId,
    DateTime CompletedAt,
    int XpAwarded,
    string? PeriodKey
);
