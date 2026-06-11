using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface ISeasonRewardService
{
    Task<SeasonRewardResponse> CreateSeasonRewardAsync(int seasonId, CreateSeasonRewardRequest request);
    Task<IEnumerable<SeasonRewardResponse>> GetSeasonRewardsAsync(int seasonId);
    Task<IEnumerable<PlayerSeasonRewardResponse>> DistributeRewardsAsync(int seasonId);
    Task<IEnumerable<PlayerSeasonRewardResponse>> GetPlayerSeasonRewardsAsync(int seasonId, int playerId);
}
