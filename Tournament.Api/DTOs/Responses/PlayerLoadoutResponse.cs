namespace Tournament.Api.DTOs.Responses;

public record PlayerLoadoutResponse(
    int PlayerId,
    int? SkinId,
    string? SkinName,
    string? AssetKey
);
