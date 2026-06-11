namespace Tournament.Api.DTOs.Responses;

public record ReplayResponse(
    int Id,
    int DuelId,
    int SchemaVersion,
    DateTime RecordedAt,
    bool IsComplete
);
