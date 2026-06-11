namespace Tournament.Api.DTOs.Responses;

public record ObjectiveResponse(int Id, int SeasonId, string Name, string Description, string ObjectiveType, int TargetValue, int XpReward, string ResetType);
