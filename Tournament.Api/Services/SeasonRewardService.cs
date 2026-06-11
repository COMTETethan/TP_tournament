using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

public class SeasonRewardService : ISeasonRewardService
{
    private readonly ISeasonService _seasonService;
    private readonly List<RewardEntity> _rewards = new();
    private readonly List<PlayerRewardEntity> _playerRewards = new();
    private int _nextRewardId = 1;
    private int _nextPlayerRewardId = 1;

    public SeasonRewardService(ISeasonService seasonService)
    {
        _seasonService = seasonService;
    }

    public async Task<SeasonRewardResponse> CreateSeasonRewardAsync(int seasonId, CreateSeasonRewardRequest request)
    {
        await _seasonService.GetSeasonAsync(seasonId);

        if (request.RankMin <= 0)
            throw new ArgumentException("RankMin must be > 0.", nameof(request.RankMin));
        if (request.RankMax.HasValue && request.RankMax.Value < request.RankMin)
            throw new ArgumentException("RankMax must be >= RankMin.", nameof(request.RankMax));
        if (string.IsNullOrWhiteSpace(request.Label))
            throw new ArgumentException("Label cannot be empty.", nameof(request.Label));

        var entity = new RewardEntity
        {
            Id = _nextRewardId++,
            SeasonId = seasonId,
            RankMin = request.RankMin,
            RankMax = request.RankMax,
            RewardType = request.RewardType,
            RewardData = request.RewardData,
            Label = request.Label
        };
        _rewards.Add(entity);
        return Map(entity);
    }

    public async Task<IEnumerable<SeasonRewardResponse>> GetSeasonRewardsAsync(int seasonId)
    {
        await _seasonService.GetSeasonAsync(seasonId);
        return _rewards.Where(r => r.SeasonId == seasonId).Select(Map).ToList();
    }

    public async Task<IEnumerable<PlayerSeasonRewardResponse>> DistributeRewardsAsync(int seasonId)
    {
        var allStats = (await _seasonService.GetAllPlayerSeasonalStatsAsync(seasonId)).ToList();
        var brackets = _rewards.Where(r => r.SeasonId == seasonId).ToList();
        var distributed = new List<PlayerRewardEntity>();

        foreach (var stats in allStats)
        {
            // RANK(): number of players with strictly higher score + 1
            int rank = allStats.Count(s => s.TotalScore > stats.TotalScore) + 1;

            foreach (var bracket in brackets)
            {
                if (rank >= bracket.RankMin && (bracket.RankMax is null || rank <= bracket.RankMax))
                {
                    if (!_playerRewards.Any(pr => pr.PlayerId == stats.PlayerId
                                               && pr.SeasonId == seasonId
                                               && pr.SeasonRewardId == bracket.Id))
                    {
                        var entity = new PlayerRewardEntity
                        {
                            Id = _nextPlayerRewardId++,
                            PlayerId = stats.PlayerId,
                            SeasonId = seasonId,
                            SeasonRank = rank,
                            SeasonRewardId = bracket.Id,
                            AwardedAt = DateTime.UtcNow
                        };
                        _playerRewards.Add(entity);
                        distributed.Add(entity);
                    }
                }
            }
        }

        return distributed.Select(MapPlayerReward).ToList();
    }

    public async Task<IEnumerable<PlayerSeasonRewardResponse>> GetPlayerSeasonRewardsAsync(int seasonId, int playerId)
    {
        await _seasonService.GetSeasonAsync(seasonId);
        return _playerRewards
            .Where(pr => pr.SeasonId == seasonId && pr.PlayerId == playerId)
            .Select(MapPlayerReward)
            .ToList();
    }

    private static SeasonRewardResponse Map(RewardEntity e)
        => new(e.Id, e.SeasonId, e.RankMin, e.RankMax, e.RewardType, e.RewardData, e.Label);

    private static PlayerSeasonRewardResponse MapPlayerReward(PlayerRewardEntity e)
        => new(e.Id, e.PlayerId, e.SeasonId, e.SeasonRank, e.SeasonRewardId, e.AwardedAt);

    private class RewardEntity
    {
        public int Id { get; set; }
        public int SeasonId { get; set; }
        public int RankMin { get; set; }
        public int? RankMax { get; set; }
        public string RewardType { get; set; } = string.Empty;
        public string RewardData { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    private class PlayerRewardEntity
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int SeasonId { get; set; }
        public int SeasonRank { get; set; }
        public int SeasonRewardId { get; set; }
        public DateTime AwardedAt { get; set; }
    }
}
