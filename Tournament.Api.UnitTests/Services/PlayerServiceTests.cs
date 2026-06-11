using Tournament.Api.DTOs.Requests;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class PlayerServiceTests
{
    private readonly PlayerService _service = new();

    private const int KnightClass = 1;

    // ── CreatePlayerAsync ──────────────────────────────────────

    [Fact]
    public async Task CreatePlayerAsync_ValidRequest_ReturnsChampionOwnedByUser()
    {
        var result = await _service.CreatePlayerAsync(userId: 7, new CreatePlayerRequest("Sir Galahad", KnightClass, Level: 3));

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(7);
        result.Name.Should().Be("Sir Galahad");
        result.ClassId.Should().Be(KnightClass);
        result.Level.Should().Be(3);
    }

    [Fact]
    public async Task CreatePlayerAsync_DefaultLevel_IsOne()
    {
        var result = await _service.CreatePlayerAsync(1, new CreatePlayerRequest("Squire", KnightClass));

        result.Level.Should().Be(1);
    }

    [Fact]
    public async Task CreatePlayerAsync_EmptyName_ThrowsArgumentException()
    {
        Func<Task> act = () => _service.CreatePlayerAsync(1, new CreatePlayerRequest("", KnightClass));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreatePlayerAsync_UnknownClass_ThrowsClassNotFoundException()
    {
        Func<Task> act = () => _service.CreatePlayerAsync(1, new CreatePlayerRequest("Ghost", ClassId: 999));

        await act.Should().ThrowAsync<ClassNotFoundException>()
                 .Where(e => e.ClassId == 999);
    }

    [Fact]
    public async Task CreatePlayerAsync_LevelBelowOne_ThrowsArgumentException()
    {
        Func<Task> act = () => _service.CreatePlayerAsync(1, new CreatePlayerRequest("Sir Galahad", KnightClass, Level: 0));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── GetPlayerAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetPlayerAsync_ExistingId_ReturnsChampion()
    {
        var created = await _service.CreatePlayerAsync(1, new CreatePlayerRequest("Sir Galahad", KnightClass));

        var result = await _service.GetPlayerAsync(created.Id);

        result.Id.Should().Be(created.Id);
        result.Name.Should().Be("Sir Galahad");
    }

    [Fact]
    public async Task GetPlayerAsync_NonExistingId_ThrowsPlayerNotFoundException()
    {
        Func<Task> act = () => _service.GetPlayerAsync(9999);

        await act.Should().ThrowAsync<PlayerNotFoundException>()
                 .Where(e => e.PlayerId == 9999);
    }

    // ── GetUserPlayersAsync ────────────────────────────────────

    [Fact]
    public async Task GetUserPlayersAsync_ReturnsOnlyThatUsersChampions()
    {
        var knight = await _service.CreatePlayerAsync(42, new CreatePlayerRequest("Knight", 1));
        var mage   = await _service.CreatePlayerAsync(42, new CreatePlayerRequest("Mage", 2));
        await _service.CreatePlayerAsync(99, new CreatePlayerRequest("Someone Else", 3));

        var result = (await _service.GetUserPlayersAsync(42)).ToList();

        result.Should().OnlyContain(p => p.UserId == 42);
        result.Select(p => p.Name).Should().Contain(new[] { "Knight", "Mage" });
        result.Should().NotContain(p => p.Name == "Someone Else");
    }

    [Fact]
    public async Task GetUserPlayersAsync_UserWithNoChampions_ReturnsEmpty()
    {
        var result = await _service.GetUserPlayersAsync(123456);

        result.Should().BeEmpty();
    }
}
