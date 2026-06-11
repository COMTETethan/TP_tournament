namespace Tournament.Api.DTOs.Requests;

public record CreateSeasonRewardRequest(
    int RankMin,
    int? RankMax,
    string RewardType,
    string RewardData,
    string Label
);
