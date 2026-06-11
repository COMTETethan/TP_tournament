namespace Tournament.Api.DTOs.Responses;

public record CosmeticSnapshotResponse(
    int DuelId,
    string? Player1SkinName,
    string? Player1AssetKey,
    string? Player2SkinName,
    string? Player2AssetKey,
    string? BackgroundSkinName,
    string? BackgroundAssetKey
);
