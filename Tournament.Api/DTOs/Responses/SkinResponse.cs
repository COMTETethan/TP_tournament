namespace Tournament.Api.DTOs.Responses;

public record SkinResponse(
    int Id,
    string Category,
    string Name,
    string AssetKey,
    bool IsPremium,
    bool IsActive,
    DateTime CreatedAt
);
