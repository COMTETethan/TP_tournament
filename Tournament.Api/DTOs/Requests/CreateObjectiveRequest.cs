namespace Tournament.Api.DTOs.Requests;

public record CreateObjectiveRequest(int SeasonId, string Name, string Description, string ObjectiveType, int TargetValue, int XpReward, string ResetType);
