namespace Tournament.Api.DTOs.Responses;

public record PlayerBattlepassProgressResponse(int Id, int PlayerId, int BattlepassId, int CurrentXp, int CurrentTier, bool IsPremiumUnlocked);
