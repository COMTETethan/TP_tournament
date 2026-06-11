namespace Tournament.Api.DTOs.Responses;

public record ReplayEventResponse(
    long Id,
    int ReplayId,
    int EventOrder,
    string EventType,
    int? ActorPlayerId,
    int? TargetPlayerId,
    int OccurredAtMs,
    string? Payload
);
