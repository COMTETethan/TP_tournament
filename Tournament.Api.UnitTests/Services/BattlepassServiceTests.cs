using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class BattlepassServiceTests
{
    private readonly BattlepassService _service = new();

    // ── CreateBattlepassAsync ──────────────────────────────────

    [Fact]
    public async Task CreateBattlepassAsync_ValidRequest_ReturnsBattlepass()
    {
        var request = new CreateBattlepassRequest(SeasonId: 1, TotalTiers: 100, HasPremiumTrack: true);

        var result = await _service.CreateBattlepassAsync(request);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.SeasonId.Should().Be(1);
        result.TotalTiers.Should().Be(100);
        result.HasPremiumTrack.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBattlepassAsync_ZeroTiers_ThrowsArgumentException()
    {
        var request = new CreateBattlepassRequest(SeasonId: 1, TotalTiers: 0, HasPremiumTrack: false);

        Func<Task> act = () => _service.CreateBattlepassAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── GetBattlepassBySeasonAsync ─────────────────────────────

    [Fact]
    public async Task GetBattlepassBySeasonAsync_ExistingBattlepass_ReturnsBattlepass()
    {
        var created = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(2, 50, false));

        var result = await _service.GetBattlepassBySeasonAsync(2);

        result.Id.Should().Be(created.Id);
        result.SeasonId.Should().Be(2);
    }

    [Fact]
    public async Task GetBattlepassBySeasonAsync_NoBattlepassForSeason_ThrowsBattlepassNotFoundException()
    {
        Func<Task> act = () => _service.GetBattlepassBySeasonAsync(9999);

        await act.Should().ThrowAsync<BattlepassNotFoundException>();
    }

    // ── AddTierAsync ───────────────────────────────────────────

    [Fact]
    public async Task AddTierAsync_ValidTier_ReturnsTier()
    {
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(3, 100, true));
        var request = new AddBattlepassTierRequest(TierNumber: 10, XpRequired: 1000, IsPremium: false, RewardType: "TITLE", RewardData: "{\"title\":\"Écuyer\"}");

        var result = await _service.AddTierAsync(bp.Id, request);

        result.Should().NotBeNull();
        result.BattlepassId.Should().Be(bp.Id);
        result.TierNumber.Should().Be(10);
        result.XpRequired.Should().Be(1000);
        result.IsPremium.Should().BeFalse();
        result.RewardType.Should().Be("TITLE");
    }

    [Fact]
    public async Task AddTierAsync_NonExistingBattlepass_ThrowsBattlepassNotFoundException()
    {
        var request = new AddBattlepassTierRequest(1, 100, false, "TITLE", "{}");

        Func<Task> act = () => _service.AddTierAsync(9999, request);

        await act.Should().ThrowAsync<BattlepassNotFoundException>()
                 .Where(e => e.BattlepassId == 9999);
    }

    // ── GetTiersAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetTiersAsync_AfterAddingTiers_ReturnsAllTiers()
    {
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(4, 100, true));
        await _service.AddTierAsync(bp.Id, new AddBattlepassTierRequest(10, 1000, false, "TITLE", "{}"));
        await _service.AddTierAsync(bp.Id, new AddBattlepassTierRequest(25, 2500, false, "SKIN",  "{}"));

        var result = await _service.GetTiersAsync(bp.Id);

        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(t => t.TierNumber);
    }

    // ── GetPlayerProgressAsync ─────────────────────────────────

    [Fact]
    public async Task GetPlayerProgressAsync_NewPlayer_ReturnsZeroProgress()
    {
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(5, 100, true));

        var result = await _service.GetPlayerProgressAsync(bp.Id, playerId: 1);

        result.Should().NotBeNull();
        result.BattlepassId.Should().Be(bp.Id);
        result.PlayerId.Should().Be(1);
        result.CurrentXp.Should().Be(0);
        result.CurrentTier.Should().Be(0);
        result.IsPremiumUnlocked.Should().BeFalse();
    }

    // ── AddXpAsync ─────────────────────────────────────────────

    [Fact]
    public async Task AddXpAsync_ValidAmount_IncreasesXp()
    {
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(6, 100, true));

        var result = await _service.AddXpAsync(bp.Id, playerId: 1, new AddXpRequest(500));

        result.CurrentXp.Should().Be(500);
    }

    [Fact]
    public async Task AddXpAsync_ReachingTierThreshold_AdvancesCurrentTier()
    {
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(7, 100, true));
        await _service.AddTierAsync(bp.Id, new AddBattlepassTierRequest(1, 100, false, "CURRENCY", "{\"amount\":50}"));

        var result = await _service.AddXpAsync(bp.Id, playerId: 1, new AddXpRequest(100));

        result.CurrentTier.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task AddXpAsync_NegativeAmount_ThrowsArgumentException()
    {
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(8, 100, true));

        Func<Task> act = () => _service.AddXpAsync(bp.Id, 1, new AddXpRequest(-10));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddXpAsync_NoTiersAdded_CurrentTierRemainsZero()
    {
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 100, true));

        var result = await _service.AddXpAsync(bp.Id, 1, new AddXpRequest(500));

        result.CurrentTier.Should().Be(0, "no tiers defined so none can be unlocked");
    }

    [Fact]
    public async Task GetPlayerProgressAsync_CalledTwice_ReturnsSameProgress()
    {
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 100, true));

        var first  = await _service.GetPlayerProgressAsync(bp.Id, 1);
        var second = await _service.GetPlayerProgressAsync(bp.Id, 1);

        second.Should().BeEquivalentTo(first);
    }

    [Fact]
    public async Task AddXpAsync_XpBelowTierThreshold_TierRemainsZero()
    {
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 200, true));
        await _service.AddTierAsync(bp.Id, new AddBattlepassTierRequest(1, 100, false, "SKIN", "{}"));

        var result = await _service.AddXpAsync(bp.Id, 1, new AddXpRequest(50));

        result.CurrentTier.Should().Be(0, "XP 50 < threshold 100 so no tier unlocked");
    }

    [Fact]
    public async Task GetPlayerProgressAsync_TwoPlayers_EachGetsOwnProgress()
    {
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 100, true));

        await _service.GetPlayerProgressAsync(bp.Id, 1);
        var p2 = await _service.GetPlayerProgressAsync(bp.Id, 2);

        p2.PlayerId.Should().Be(2);
        p2.CurrentXp.Should().Be(0);
    }

    [Fact]
    public async Task GetPlayerProgressAsync_TwoBattlepasses_EachGetsOwnProgress()
    {
        var bp1 = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 100, true));
        var bp2 = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 200, true));

        await _service.GetPlayerProgressAsync(bp1.Id, 1);
        var result = await _service.GetPlayerProgressAsync(bp2.Id, 1);

        result.BattlepassId.Should().Be(bp2.Id);
        result.CurrentXp.Should().Be(0);
    }

    [Fact]
    public async Task AddXpAsync_TierBelongsToOtherBattlepass_IsNotUnlocked()
    {
        var bp1 = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 200, true));
        var bp2 = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 200, true));
        await _service.AddTierAsync(bp1.Id, new AddBattlepassTierRequest(1, 10, false, "SKIN", "{}"));

        var result = await _service.AddXpAsync(bp2.Id, 1, new AddXpRequest(500));

        result.CurrentTier.Should().Be(0, "tier belongs to bp1 not bp2");
    }
}
