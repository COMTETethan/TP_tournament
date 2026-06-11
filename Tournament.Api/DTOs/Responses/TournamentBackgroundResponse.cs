namespace Tournament.Api.DTOs.Responses;

public record TournamentBackgroundResponse(
    int TournamentId,
    int? SkinId,
    string? SkinName,
    string? AssetKey
);
