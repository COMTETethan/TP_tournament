namespace Tournament.Api.DTOs.Responses;

public record PlayerObjectiveProgressResponse(int ObjectiveId, int PlayerId, int CurrentValue, bool IsCompleted, DateTime? CompletedAt);
