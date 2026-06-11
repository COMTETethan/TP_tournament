using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

public class BattlepassService : IBattlepassService
{
    private readonly List<BattlepassEntity> _battlepasses = new();
    private readonly List<TierEntity> _tiers = new();
    private readonly List<ProgressEntity> _progress = new();
    private int _nextBpId = 1;
    private int _nextTierId = 1;
    private int _nextProgressId = 1;

    public Task<BattlepassResponse> CreateBattlepassAsync(CreateBattlepassRequest request)
    {
        if (request.TotalTiers <= 0)
            throw new ArgumentException("TotalTiers must be > 0.", nameof(request.TotalTiers));

        var entity = new BattlepassEntity
        {
            Id = _nextBpId++,
            SeasonId = request.SeasonId,
            TotalTiers = request.TotalTiers,
            HasPremiumTrack = request.HasPremiumTrack
        };
        _battlepasses.Add(entity);
        return Task.FromResult(Map(entity));
    }

    public Task<BattlepassResponse> GetBattlepassBySeasonAsync(int seasonId)
    {
        var entity = _battlepasses.FirstOrDefault(b => b.SeasonId == seasonId)
            ?? throw new BattlepassNotFoundException(seasonId);
        return Task.FromResult(Map(entity));
    }

    public Task<BattlepassTierResponse> AddTierAsync(int battlepassId, AddBattlepassTierRequest request)
    {
        if (!_battlepasses.Any(b => b.Id == battlepassId))
            throw new BattlepassNotFoundException(battlepassId);

        var tier = new TierEntity
        {
            Id = _nextTierId++,
            BattlepassId = battlepassId,
            TierNumber = request.TierNumber,
            XpRequired = request.XpRequired,
            IsPremium = request.IsPremium,
            RewardType = request.RewardType,
            RewardData = request.RewardData
        };
        _tiers.Add(tier);
        return Task.FromResult(MapTier(tier));
    }

    public Task<IEnumerable<BattlepassTierResponse>> GetTiersAsync(int battlepassId)
    {
        var result = _tiers
            .Where(t => t.BattlepassId == battlepassId)
            .OrderBy(t => t.TierNumber)
            .Select(MapTier)
            .ToList();
        return Task.FromResult<IEnumerable<BattlepassTierResponse>>(result);
    }

    public Task<PlayerBattlepassProgressResponse> GetPlayerProgressAsync(int battlepassId, int playerId)
    {
        if (!_battlepasses.Any(b => b.Id == battlepassId))
            throw new BattlepassNotFoundException(battlepassId);

        return Task.FromResult(MapProgress(GetOrCreateProgress(battlepassId, playerId)));
    }

    public Task<PlayerBattlepassProgressResponse> AddXpAsync(int battlepassId, int playerId, AddXpRequest request)
    {
        if (request.XpAmount < 0)
            throw new ArgumentException("XP must be positive.", nameof(request.XpAmount));

        if (!_battlepasses.Any(b => b.Id == battlepassId))
            throw new BattlepassNotFoundException(battlepassId);

        var prog = GetOrCreateProgress(battlepassId, playerId);
        prog.CurrentXp += request.XpAmount;

        var unlockedTiers = _tiers
            .Where(t => t.BattlepassId == battlepassId && t.XpRequired <= prog.CurrentXp)
            .Select(t => t.TierNumber)
            .ToList();

        prog.CurrentTier = unlockedTiers.Count != 0 ? unlockedTiers.Max() : 0;

        return Task.FromResult(MapProgress(prog));
    }

    private ProgressEntity GetOrCreateProgress(int battlepassId, int playerId)
    {
        var existing = _progress.FirstOrDefault(p => p.BattlepassId == battlepassId && p.PlayerId == playerId);
        if (existing is not null) return existing;

        var prog = new ProgressEntity
        {
            Id = _nextProgressId++,
            BattlepassId = battlepassId,
            PlayerId = playerId
        };
        _progress.Add(prog);
        return prog;
    }

    private static BattlepassResponse Map(BattlepassEntity e)
        => new(e.Id, e.SeasonId, e.TotalTiers, e.HasPremiumTrack);

    private static BattlepassTierResponse MapTier(TierEntity e)
        => new(e.Id, e.BattlepassId, e.TierNumber, e.XpRequired, e.IsPremium, e.RewardType, e.RewardData);

    private static PlayerBattlepassProgressResponse MapProgress(ProgressEntity e)
        => new(e.Id, e.PlayerId, e.BattlepassId, e.CurrentXp, e.CurrentTier, e.IsPremiumUnlocked);

    private class BattlepassEntity
    {
        public int Id { get; set; }
        public int SeasonId { get; set; }
        public int TotalTiers { get; set; }
        public bool HasPremiumTrack { get; set; }
    }

    private class TierEntity
    {
        public int Id { get; set; }
        public int BattlepassId { get; set; }
        public int TierNumber { get; set; }
        public int XpRequired { get; set; }
        public bool IsPremium { get; set; }
        public string RewardType { get; set; } = string.Empty;
        public string RewardData { get; set; } = string.Empty;
    }

    private class ProgressEntity
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int BattlepassId { get; set; }
        public int CurrentXp { get; set; }
        public int CurrentTier { get; set; }
        public bool IsPremiumUnlocked { get; set; }
    }
}
