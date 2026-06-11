namespace Tournament.Api.DTOs.Responses;

public record BattlepassResponse(int Id, int SeasonId, int TotalTiers, bool HasPremiumTrack);
