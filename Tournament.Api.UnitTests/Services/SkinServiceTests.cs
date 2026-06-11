using FluentAssertions;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

[Trait("Category", "Skin")]
[Trait("Layer", "Service")]
public class SkinServiceTests
{
    private readonly SkinService _service = new();

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
        result.IsPremium.Should().BeFalse();
        result.AssetKey.Should().Be("skins/player/dark_knight");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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
        var skin = await _service.CreateSkinAsync(new CreateSkinRequest("BACKGROUND", "Stone Z", "skins/bg/stone_z"));
        var request = new EquipPlayerSkinRequest(skin.Id);

        // Act
        Func<Task> act = () => _service.EquipPlayerSkinAsync(playerId: 1, request);

        // Assert
        await act.Should().ThrowAsync<InvalidSkinCategoryException>();
    }

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
        var skin    = await _service.CreateSkinAsync(new CreateSkinRequest("PLAYER", "Knight Z", "skins/player/knight_z"));
        var request = new SetTournamentBackgroundRequest(skin.Id);

        // Act
        Func<Task> act = () => _service.SetTournamentBackgroundAsync(tournamentId: 1, request);

        // Assert
        await act.Should().ThrowAsync<InvalidSkinCategoryException>();
    }

    [Fact]
    public async Task SetTournamentBackgroundAsync_CalledTwice_UpdatesExistingBackground()
    {
        // Arrange
        var skinA = await _service.CreateSkinAsync(new CreateSkinRequest("BACKGROUND", "Arena X", "skins/bg/arena_x"));
        var skinB = await _service.CreateSkinAsync(new CreateSkinRequest("BACKGROUND", "Arena Y", "skins/bg/arena_y"));
        await _service.SetTournamentBackgroundAsync(tournamentId: 42, new SetTournamentBackgroundRequest(skinA.Id));

        // Act
        var result = await _service.SetTournamentBackgroundAsync(tournamentId: 42, new SetTournamentBackgroundRequest(skinB.Id));

        // Assert
        result.SkinId.Should().Be(skinB.Id);
        result.SkinName.Should().Be("Arena Y");
        result.AssetKey.Should().Be("skins/bg/arena_y");
    }

    [Fact]
    public async Task EquipPlayerSkinAsync_CalledTwice_UpdatesExistingLoadout()
    {
        // Arrange
        var skinA = await _service.CreateSkinAsync(new CreateSkinRequest("PLAYER", "Warrior A", "skins/player/warrior_a"));
        var skinB = await _service.CreateSkinAsync(new CreateSkinRequest("PLAYER", "Warrior B", "skins/player/warrior_b"));
        await _service.EquipPlayerSkinAsync(playerId: 99, new EquipPlayerSkinRequest(skinA.Id));

        // Act
        var result = await _service.EquipPlayerSkinAsync(playerId: 99, new EquipPlayerSkinRequest(skinB.Id));

        // Assert
        result.SkinId.Should().Be(skinB.Id);
        result.SkinName.Should().Be("Warrior B");
        result.AssetKey.Should().Be("skins/player/warrior_b");
    }

    [Fact]
    public async Task GetPlayerLoadoutAsync_PlayerWithLoadout_ReturnsLoadoutWithSkinDetails()
    {
        // Arrange
        var skin = await _service.CreateSkinAsync(new CreateSkinRequest("PLAYER", "Shadow Knight", "skins/player/shadow"));
        await _service.EquipPlayerSkinAsync(playerId: 77, new EquipPlayerSkinRequest(skin.Id));

        // Act
        var result = await _service.GetPlayerLoadoutAsync(playerId: 77);

        // Assert
        result.PlayerId.Should().Be(77);
        result.SkinId.Should().Be(skin.Id);
        result.SkinName.Should().Be("Shadow Knight");
        result.AssetKey.Should().Be("skins/player/shadow");
    }

    [Fact]
    public async Task GetPlayerLoadoutAsync_PlayerWithNoLoadout_ReturnsEmptyLoadout()
    {
        var result = await _service.GetPlayerLoadoutAsync(playerId: 88);

        // Assert
        result.PlayerId.Should().Be(88);
        result.SkinId.Should().BeNull();
        result.SkinName.Should().BeNull();
        result.AssetKey.Should().BeNull();
    }

    [Fact]
    public async Task GetTournamentBackgroundAsync_TournamentWithBackground_ReturnsBackground()
    {
        // Arrange
        var skin = await _service.CreateSkinAsync(new CreateSkinRequest("BACKGROUND", "Lava Arena", "skins/bg/lava"));
        await _service.SetTournamentBackgroundAsync(tournamentId: 55, new SetTournamentBackgroundRequest(skin.Id));

        // Act
        var result = await _service.GetTournamentBackgroundAsync(tournamentId: 55);

        // Assert
        result.TournamentId.Should().Be(55);
        result.SkinId.Should().Be(skin.Id);
        result.SkinName.Should().Be("Lava Arena");
        result.AssetKey.Should().Be("skins/bg/lava");
    }

    [Fact]
    public async Task GetTournamentBackgroundAsync_TournamentWithNoBackground_ReturnsEmptyBackground()
    {
        var result = await _service.GetTournamentBackgroundAsync(tournamentId: 999);

        // Assert
        result.TournamentId.Should().Be(999);
        result.SkinId.Should().BeNull();
        result.SkinName.Should().BeNull();
        result.AssetKey.Should().BeNull();
    }
}
