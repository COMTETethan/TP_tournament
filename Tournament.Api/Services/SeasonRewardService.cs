using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Services;

public class SeasonRewardService : ISeasonRewardService
{
    private readonly ISeasonService _seasonService;

    public SeasonRewardService(ISeasonService seasonService)
    {
        _seasonService = seasonService;
    }

    public Task<SeasonRewardResponse> CreateSeasonRewardAsync(int seasonId, CreateSeasonRewardRequest request)
        => throw new NotImplementedException();

    public Task<IEnumerable<SeasonRewardResponse>> GetSeasonRewardsAsync(int seasonId)
        => throw new NotImplementedException();

    public Task<IEnumerable<PlayerSeasonRewardResponse>> DistributeRewardsAsync(int seasonId)
        => throw new NotImplementedException();

    public Task<IEnumerable<PlayerSeasonRewardResponse>> GetPlayerSeasonRewardsAsync(int seasonId, int playerId)
        => throw new NotImplementedException();
}
