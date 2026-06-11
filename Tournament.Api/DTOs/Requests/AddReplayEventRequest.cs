namespace Tournament.Api.DTOs.Requests;

public record AddReplayEventRequest(
    string EventType,
    int OccurredAtMs,
    int? ActorPlayerId,
    int? TargetPlayerId,
    string? Payload
);
