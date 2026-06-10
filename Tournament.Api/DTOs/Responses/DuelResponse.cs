namespace Tournament.Api.DTOs.Responses;

public record DuelResponse(
    int Id,
    int TournamentId,
    int Player1Id,
    int Player2Id,
    string? Outcome,
    int DuelOrder,
    DateTime PlayedAt,
    int? DurationSeconds);
