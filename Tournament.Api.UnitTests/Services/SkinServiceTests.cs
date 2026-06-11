using FluentAssertions;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class SkinServiceTests
{
    private readonly SkinService _service = new();

    // ── CreateSkinAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSkinAsync_ValidPlayerSkin_ReturnsSkinResponse()
    {
        // Arrange
        var request = new CreateSkinRequest("PLAYER", "Dark Knight", "skins/player/dark_knight");

        // Act
        var result = await _service.CreateSkinAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Dark Knight");
        result.Category.Should().Be("PLAYER");
        result.IsActive.Should().BeTrue();
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateSkinAsync_InvalidCategory_ThrowsInvalidSkinCategoryException()
    {
        // Arrange
        var request = new CreateSkinRequest("WEAPON", "Sword", "skins/weapon/sword");

        // Act
        Func<Task> act = () => _service.CreateSkinAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidSkinCategoryException>()
                 .Where(e => e.AttemptedCategory == "WEAPON");
    }

    // ── GetAllSkinsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllSkinsAsync_AfterCreatingTwo_ReturnsBothSkins()
    {
        // Arrange
        await _service.CreateSkinAsync(new CreateSkinRequest("PLAYER",     "Knight A", "skins/player/knight_a"));
        await _service.CreateSkinAsync(new CreateSkinRequest("BACKGROUND", "Arena B",  "skins/bg/arena_b"));

        // Act
        var result = await _service.GetAllSkinsAsync();

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ── GetSkinAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSkinAsync_ExistingId_ReturnsSkinResponse()
    {
        // Arrange
        var created = await _service.CreateSkinAsync(new CreateSkinRequest("PLAYER", "Knight X", "skins/player/knight_x"));

        // Act
        var result = await _service.GetSkinAsync(created.Id);

        // Assert
        result.Id.Should().Be(created.Id);
        result.Name.Should().Be("Knight X");
    }

    [Fact]
    public async Task GetSkinAsync_NonExistingId_ThrowsSkinNotFoundException()
    {
        // Arrange
        const int nonExistingId = 9999;

        // Act
        Func<Task> act = () => _service.GetSkinAsync(nonExistingId);

        // Assert
        await act.Should().ThrowAsync<SkinNotFoundException>()
                 .Where(e => e.SkinId == nonExistingId);
    }

    // ── DeactivateSkinAsync ────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateSkinAsync_ExistingId_SetsIsActiveFalse()
    {
        // Arrange
        var created = await _service.CreateSkinAsync(new CreateSkinRequest("BACKGROUND", "Ghost Castle", "skins/bg/ghost_castle"));

        // Act
        await _service.DeactivateSkinAsync(created.Id);
        var deactivated = await _service.GetSkinAsync(created.Id);

        // Assert
        deactivated.IsActive.Should().BeFalse();
    }

    // ── EquipPlayerSkinAsync ───────────────────────────────────────────────

    [Fact]
    public async Task EquipPlayerSkinAsync_ValidPlayerAndPlayerSkin_ReturnsLoadout()
    {
        // Arrange
        var skin = await _service.CreateSkinAsync(new CreateSkinRequest("PLAYER", "Knight Y", "skins/player/knight_y"));
        var request = new EquipPlayerSkinRequest(skin.Id);

        // Act
        var result = await _service.EquipPlayerSkinAsync(playerId: 1, request);

        // Assert
        result.Should().NotBeNull();
        result.PlayerId.Should().Be(1);
        result.SkinId.Should().Be(skin.Id);
    }

    [Fact]
    public async Task EquipPlayerSkinAsync_NonExistingSkin_ThrowsSkinNotFoundException()
    {
        // Arrange
        var request = new EquipPlayerSkinRequest(SkinId: 9999);

        // Act
        Func<Task> act = () => _service.EquipPlayerSkinAsync(playerId: 1, request);

        // Assert
        await act.Should().ThrowAsync<SkinNotFoundException>()
                 .Where(e => e.SkinId == 9999);
    }

    [Fact]
    public async Task EquipPlayerSkinAsync_BackgroundSkinOnPlayer_ThrowsInvalidSkinCategoryException()
    {
        // Arrange — background skins cannot be equipped on a player
        var skin = await _service.CreateSkinAsync(new CreateSkinRequest("BACKGROUND", "Stone Z", "skins/bg/stone_z"));
        var request = new EquipPlayerSkinRequest(skin.Id);

        // Act
        Func<Task> act = () => _service.EquipPlayerSkinAsync(playerId: 1, request);

        // Assert
        await act.Should().ThrowAsync<InvalidSkinCategoryException>();
    }

    // ── SetTournamentBackgroundAsync ───────────────────────────────────────

    [Fact]
    public async Task SetTournamentBackgroundAsync_ValidBackgroundSkin_ReturnsTournamentBackground()
    {
        // Arrange
        var skin    = await _service.CreateSkinAsync(new CreateSkinRequest("BACKGROUND", "Arena C", "skins/bg/arena_c"));
        var request = new SetTournamentBackgroundRequest(skin.Id);

        // Act
        var result = await _service.SetTournamentBackgroundAsync(tournamentId: 1, request);

        // Assert
        result.Should().NotBeNull();
        result.TournamentId.Should().Be(1);
        result.SkinId.Should().Be(skin.Id);
    }

    [Fact]
    public async Task SetTournamentBackgroundAsync_PlayerSkinOnBackground_ThrowsInvalidSkinCategoryException()
    {
        // Arrange — player skins cannot be used as background
        var skin    = await _service.CreateSkinAsync(new CreateSkinRequest("PLAYER", "Knight Z", "skins/player/knight_z"));
        var request = new SetTournamentBackgroundRequest(skin.Id);

        // Act
        Func<Task> act = () => _service.SetTournamentBackgroundAsync(tournamentId: 1, request);

        // Assert
        await act.Should().ThrowAsync<InvalidSkinCategoryException>();
    }
}
