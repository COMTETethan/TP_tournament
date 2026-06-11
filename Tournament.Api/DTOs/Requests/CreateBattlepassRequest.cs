namespace Tournament.Api.DTOs.Requests;

public record CreateBattlepassRequest(int SeasonId, int TotalTiers, bool HasPremiumTrack);
