namespace Tournament.Api.DTOs.Responses;

public record PlayerSeasonRewardResponse(
    int Id,
    int PlayerId,
    int SeasonId,
    int SeasonRank,
    int SeasonRewardId,
    DateTime AwardedAt
);
