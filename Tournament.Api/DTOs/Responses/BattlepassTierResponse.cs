namespace Tournament.Api.DTOs.Responses;

public record BattlepassTierResponse(int Id, int BattlepassId, int TierNumber, int XpRequired, bool IsPremium, string RewardType, string RewardData);
