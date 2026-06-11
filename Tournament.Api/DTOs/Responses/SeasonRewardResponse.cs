namespace Tournament.Api.DTOs.Responses;

public record SeasonRewardResponse(
    int Id,
    int SeasonId,
    int RankMin,
    int? RankMax,
    string RewardType,
    string RewardData,
    string Label
);
