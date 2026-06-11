namespace Tournament.Api.DTOs.Requests;

public record CreateSkinRequest(
    string Category,
    string Name,
    string AssetKey,
    bool IsPremium = false
);
