namespace Tournament.Api.DTOs.Requests;

public record AddBattlepassTierRequest(int TierNumber, int XpRequired, bool IsPremium, string RewardType, string RewardData);
