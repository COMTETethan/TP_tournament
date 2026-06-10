namespace Tournament.Api.DTOs.Responses;

public record PlayerResponse(int Id, int TournamentId, string Name, bool IsDisqualified, int PenaltyPoints);
