using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

[Trait("Category", "Battlepass")]
[Trait("Layer", "Service")]
public class BattlepassServiceTests
{
    private readonly BattlepassService _service = new();

    [Fact]
    public async Task CreateBattlepassAsync_ValidRequest_ReturnsBattlepass()
    {
        // Arrange
        var request = new CreateBattlepassRequest(SeasonId: 1, TotalTiers: 100, HasPremiumTrack: true);

        // Act
        var result = await _service.CreateBattlepassAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.SeasonId.Should().Be(1);
        result.TotalTiers.Should().Be(100);
        result.HasPremiumTrack.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBattlepassAsync_ZeroTiers_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateBattlepassRequest(SeasonId: 1, TotalTiers: 0, HasPremiumTrack: false);

        // Act
        Func<Task> act = () => _service.CreateBattlepassAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetBattlepassBySeasonAsync_ExistingBattlepass_ReturnsBattlepass()
    {
        // Arrange
        var created = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(2, 50, false));

        // Act
        var result = await _service.GetBattlepassBySeasonAsync(2);

        // Assert
        result.Id.Should().Be(created.Id);
        result.SeasonId.Should().Be(2);
    }

    [Fact]
    public async Task GetBattlepassBySeasonAsync_NoBattlepassForSeason_ThrowsBattlepassNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.GetBattlepassBySeasonAsync(9999);

        // Assert
        await act.Should().ThrowAsync<BattlepassNotFoundException>();
    }

    [Fact]
    public async Task AddTierAsync_ValidTier_ReturnsTier()
    {
        // Arrange
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(3, 100, true));
        var request = new AddBattlepassTierRequest(TierNumber: 10, XpRequired: 1000, IsPremium: false, RewardType: "TITLE", RewardData: "{\"title\":\"Écuyer\"}");

        // Act
        var result = await _service.AddTierAsync(bp.Id, request);

        // Assert
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
        // Arrange
        var request = new AddBattlepassTierRequest(1, 100, false, "TITLE", "{}");

        // Act
        Func<Task> act = () => _service.AddTierAsync(9999, request);

        // Assert
        await act.Should().ThrowAsync<BattlepassNotFoundException>()
                 .Where(e => e.BattlepassId == 9999);
    }

    [Fact]
    public async Task GetTiersAsync_AfterAddingTiers_ReturnsAllTiers()
    {
        // Arrange
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(4, 100, true));
        await _service.AddTierAsync(bp.Id, new AddBattlepassTierRequest(10, 1000, false, "TITLE", "{}"));
        await _service.AddTierAsync(bp.Id, new AddBattlepassTierRequest(25, 2500, false, "SKIN",  "{}"));

        // Act
        var result = await _service.GetTiersAsync(bp.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(t => t.TierNumber);
    }

    [Fact]
    public async Task GetPlayerProgressAsync_NewPlayer_ReturnsZeroProgress()
    {
        // Arrange
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(5, 100, true));

        // Act
        var result = await _service.GetPlayerProgressAsync(bp.Id, playerId: 1);

        // Assert
        result.Should().NotBeNull();
        result.BattlepassId.Should().Be(bp.Id);
        result.PlayerId.Should().Be(1);
        result.CurrentXp.Should().Be(0);
        result.CurrentTier.Should().Be(0);
        result.IsPremiumUnlocked.Should().BeFalse();
    }

    [Fact]
    public async Task AddXpAsync_ValidAmount_IncreasesXp()
    {
        // Arrange
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(6, 100, true));

        // Act
        var result = await _service.AddXpAsync(bp.Id, playerId: 1, new AddXpRequest(500));

        // Assert
        result.CurrentXp.Should().Be(500);
    }

    [Fact]
    public async Task AddXpAsync_ReachingTierThreshold_AdvancesCurrentTier()
    {
        // Arrange
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(7, 100, true));
        await _service.AddTierAsync(bp.Id, new AddBattlepassTierRequest(1, 100, false, "CURRENCY", "{\"amount\":50}"));

        // Act
        var result = await _service.AddXpAsync(bp.Id, playerId: 1, new AddXpRequest(100));

        // Assert
        result.CurrentTier.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task AddXpAsync_NegativeAmount_ThrowsArgumentException()
    {
        // Arrange
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(8, 100, true));

        // Act
        Func<Task> act = () => _service.AddXpAsync(bp.Id, 1, new AddXpRequest(-10));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddXpAsync_NoTiersAdded_CurrentTierRemainsZero()
    {
        // Arrange
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 100, true));

        // Act
        var result = await _service.AddXpAsync(bp.Id, 1, new AddXpRequest(500));

        // Assert
        result.CurrentTier.Should().Be(0, "no tiers defined so none can be unlocked");
    }

    [Fact]
    public async Task GetPlayerProgressAsync_CalledTwice_ReturnsSameProgress()
    {
        // Arrange
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 100, true));

        var first  = await _service.GetPlayerProgressAsync(bp.Id, 1);
        // Act
        var second = await _service.GetPlayerProgressAsync(bp.Id, 1);

        // Assert
        second.Should().BeEquivalentTo(first);
    }

    [Fact]
    public async Task AddXpAsync_XpBelowTierThreshold_TierRemainsZero()
    {
        // Arrange
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 200, true));
        await _service.AddTierAsync(bp.Id, new AddBattlepassTierRequest(1, 100, false, "SKIN", "{}"));

        // Act
        var result = await _service.AddXpAsync(bp.Id, 1, new AddXpRequest(50));

        // Assert
        result.CurrentTier.Should().Be(0, "XP 50 < threshold 100 so no tier unlocked");
    }

    [Fact]
    public async Task GetPlayerProgressAsync_TwoPlayers_EachGetsOwnProgress()
    {
        // Arrange
        var bp = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 100, true));

        await _service.GetPlayerProgressAsync(bp.Id, 1);
        // Act
        var p2 = await _service.GetPlayerProgressAsync(bp.Id, 2);

        // Assert
        p2.PlayerId.Should().Be(2);
        p2.CurrentXp.Should().Be(0);
    }

    [Fact]
    public async Task GetPlayerProgressAsync_TwoBattlepasses_EachGetsOwnProgress()
    {
        // Arrange
        var bp1 = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 100, true));
        var bp2 = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 200, true));

        await _service.GetPlayerProgressAsync(bp1.Id, 1);
        // Act
        var result = await _service.GetPlayerProgressAsync(bp2.Id, 1);

        // Assert
        result.BattlepassId.Should().Be(bp2.Id);
        result.CurrentXp.Should().Be(0);
    }

    [Fact]
    public async Task AddXpAsync_TierBelongsToOtherBattlepass_IsNotUnlocked()
    {
        // Arrange
        var bp1 = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 200, true));
        var bp2 = await _service.CreateBattlepassAsync(new CreateBattlepassRequest(1, 200, true));
        await _service.AddTierAsync(bp1.Id, new AddBattlepassTierRequest(1, 10, false, "SKIN", "{}"));

        // Act
        var result = await _service.AddXpAsync(bp2.Id, 1, new AddXpRequest(500));

        // Assert
        result.CurrentTier.Should().Be(0, "tier belongs to bp1 not bp2");
    }
}
