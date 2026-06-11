using Tournament.Api.DTOs.Requests;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

[Trait("Category", "Registration")]
[Trait("Layer", "Service")]
public class TournamentPlayerServiceTests
{
    private readonly TournamentPlayerService _service = new();

    private static async Task<int> NewTournamentAsync(string name = "Tournoi")
        => (await new TournamentService().CreateTournamentAsync(new CreateTournamentRequest(name))).Id;

    [Fact]
    public async Task RegisterAsync_ValidPlayerAndTournament_ReturnsCleanRegistration()
    {
        // Arrange
        var tournamentId = await NewTournamentAsync();

        // Act
        var reg = await _service.RegisterAsync(tournamentId, playerId: 1);

        // Assert
        reg.TournamentId.Should().Be(tournamentId);
        reg.PlayerId.Should().Be(1);
        reg.PlayerName.Should().Be("Player One");
        reg.IsDisqualified.Should().BeFalse();
        reg.PenaltyPoints.Should().Be(0);
    }

    [Fact]
    public async Task RegisterAsync_UnknownTournament_ThrowsTournamentNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.RegisterAsync(9999, 1);

        // Assert
        await act.Should().ThrowAsync<TournamentNotFoundException>();
    }

    [Fact]
    public async Task RegisterAsync_UnknownPlayer_ThrowsPlayerNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.RegisterAsync(1, 9999);

        // Assert
        await act.Should().ThrowAsync<PlayerNotFoundException>();
    }

    [Fact]
    public async Task RegisterAsync_AlreadyRegistered_ThrowsPlayerAlreadyRegisteredException()
    {
        // Act
        Func<Task> act = () => _service.RegisterAsync(1, 1);

        // Assert
        await act.Should().ThrowAsync<PlayerAlreadyRegisteredException>()
                 .Where(e => e.TournamentId == 1 && e.PlayerId == 1);
    }

    [Fact]
    public async Task GetTournamentPlayersAsync_SeededTournament_ReturnsItsRegistrations()
    {
        // Act
        var result = (await _service.GetTournamentPlayersAsync(1)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(r => r.PlayerId).Should().Contain(new[] { 1, 2 });
    }

    [Fact]
    public async Task GetTournamentPlayersAsync_UnknownTournament_ThrowsTournamentNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.GetTournamentPlayersAsync(9999);

        // Assert
        await act.Should().ThrowAsync<TournamentNotFoundException>();
    }

    [Fact]
    public async Task GetRegistrationAsync_Existing_ReturnsIt()
    {
        // Act
        var reg = await _service.GetRegistrationAsync(1, 2);

        // Assert
        reg.PlayerId.Should().Be(2);
        reg.IsDisqualified.Should().BeTrue("player 2 is seeded as disqualified in tournament 1");
    }

    [Fact]
    public async Task GetRegistrationAsync_NotRegistered_ThrowsRegistrationNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.GetRegistrationAsync(1, 9999);

        // Assert
        await act.Should().ThrowAsync<RegistrationNotFoundException>()
                 .Where(e => e.TournamentId == 1 && e.PlayerId == 9999);
    }

    [Fact]
    public async Task DisqualifyAsync_RegisteredPlayer_SetsDisqualifiedAndResetsPenalties()
    {
        // Arrange
        await _service.AddPenaltyAsync(1, 1, new AddPenaltyRequest(5));

        // Act
        var reg = await _service.DisqualifyAsync(1, 1);

        // Assert
        reg.IsDisqualified.Should().BeTrue();
        reg.PenaltyPoints.Should().Be(0);
    }

    [Fact]
    public async Task DisqualifyAsync_NotRegistered_ThrowsRegistrationNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.DisqualifyAsync(1, 9999);

        // Assert
        await act.Should().ThrowAsync<RegistrationNotFoundException>();
    }

    [Fact]
    public async Task AddPenaltyAsync_ValidPenalty_Accumulates()
    {
        // Arrange
        await _service.AddPenaltyAsync(1, 1, new AddPenaltyRequest(2));
        // Act
        var reg = await _service.AddPenaltyAsync(1, 1, new AddPenaltyRequest(3));

        // Assert
        reg.PenaltyPoints.Should().Be(5);
    }

    [Fact]
    public async Task AddPenaltyAsync_Negative_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = () => _service.AddPenaltyAsync(1, 1, new AddPenaltyRequest(-1));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddPenaltyAsync_NotRegistered_ThrowsRegistrationNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.AddPenaltyAsync(1, 9999, new AddPenaltyRequest(1));

        // Assert
        await act.Should().ThrowAsync<RegistrationNotFoundException>();
    }

    [Fact]
    public async Task Disqualification_IsScopedToOneTournament()
    {
        // Arrange
        var otherTournament = await NewTournamentAsync("Autre");
        await _service.RegisterAsync(otherTournament, 1);

        // Act
        await _service.DisqualifyAsync(1, 1);

        // Assert
        (await _service.GetRegistrationAsync(1, 1)).IsDisqualified.Should().BeTrue();
        (await _service.GetRegistrationAsync(otherTournament, 1)).IsDisqualified
            .Should().BeFalse("a champion disqualified in one tournament stays active in another");
    }
}
