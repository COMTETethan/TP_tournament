using Tournament.Api.DTOs.Requests;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class TournamentPlayerServiceTests
{
    private readonly TournamentPlayerService _service = new();

    // Seed: players 1 (active) and 2 (disqualified) are registered in tournament 1.
    private static async Task<int> NewTournamentAsync(string name = "Tournoi")
        => (await new TournamentService().CreateTournamentAsync(new CreateTournamentRequest(name))).Id;

    // ── RegisterAsync ──────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ValidPlayerAndTournament_ReturnsCleanRegistration()
    {
        var tournamentId = await NewTournamentAsync();

        var reg = await _service.RegisterAsync(tournamentId, playerId: 1);

        reg.TournamentId.Should().Be(tournamentId);
        reg.PlayerId.Should().Be(1);
        reg.PlayerName.Should().Be("Player One");
        reg.IsDisqualified.Should().BeFalse();
        reg.PenaltyPoints.Should().Be(0);
    }

    [Fact]
    public async Task RegisterAsync_UnknownTournament_ThrowsTournamentNotFoundException()
    {
        Func<Task> act = () => _service.RegisterAsync(9999, 1);

        await act.Should().ThrowAsync<TournamentNotFoundException>();
    }

    [Fact]
    public async Task RegisterAsync_UnknownPlayer_ThrowsPlayerNotFoundException()
    {
        Func<Task> act = () => _service.RegisterAsync(1, 9999);

        await act.Should().ThrowAsync<PlayerNotFoundException>();
    }

    [Fact]
    public async Task RegisterAsync_AlreadyRegistered_ThrowsPlayerAlreadyRegisteredException()
    {
        // Player 1 is already registered in tournament 1 by the seed.
        Func<Task> act = () => _service.RegisterAsync(1, 1);

        await act.Should().ThrowAsync<PlayerAlreadyRegisteredException>()
                 .Where(e => e.TournamentId == 1 && e.PlayerId == 1);
    }

    // ── GetTournamentPlayersAsync ──────────────────────────────

    [Fact]
    public async Task GetTournamentPlayersAsync_SeededTournament_ReturnsItsRegistrations()
    {
        var result = (await _service.GetTournamentPlayersAsync(1)).ToList();

        result.Should().HaveCount(2);
        result.Select(r => r.PlayerId).Should().Contain(new[] { 1, 2 });
    }

    [Fact]
    public async Task GetTournamentPlayersAsync_UnknownTournament_ThrowsTournamentNotFoundException()
    {
        Func<Task> act = () => _service.GetTournamentPlayersAsync(9999);

        await act.Should().ThrowAsync<TournamentNotFoundException>();
    }

    // ── GetRegistrationAsync ───────────────────────────────────

    [Fact]
    public async Task GetRegistrationAsync_Existing_ReturnsIt()
    {
        var reg = await _service.GetRegistrationAsync(1, 2);

        reg.PlayerId.Should().Be(2);
        reg.IsDisqualified.Should().BeTrue("player 2 is seeded as disqualified in tournament 1");
    }

    [Fact]
    public async Task GetRegistrationAsync_NotRegistered_ThrowsRegistrationNotFoundException()
    {
        Func<Task> act = () => _service.GetRegistrationAsync(1, 9999);

        await act.Should().ThrowAsync<RegistrationNotFoundException>()
                 .Where(e => e.TournamentId == 1 && e.PlayerId == 9999);
    }

    // ── DisqualifyAsync ────────────────────────────────────────

    [Fact]
    public async Task DisqualifyAsync_RegisteredPlayer_SetsDisqualifiedAndResetsPenalties()
    {
        await _service.AddPenaltyAsync(1, 1, new AddPenaltyRequest(5));

        var reg = await _service.DisqualifyAsync(1, 1);

        reg.IsDisqualified.Should().BeTrue();
        reg.PenaltyPoints.Should().Be(0);
    }

    [Fact]
    public async Task DisqualifyAsync_NotRegistered_ThrowsRegistrationNotFoundException()
    {
        Func<Task> act = () => _service.DisqualifyAsync(1, 9999);

        await act.Should().ThrowAsync<RegistrationNotFoundException>();
    }

    // ── AddPenaltyAsync ────────────────────────────────────────

    [Fact]
    public async Task AddPenaltyAsync_ValidPenalty_Accumulates()
    {
        await _service.AddPenaltyAsync(1, 1, new AddPenaltyRequest(2));
        var reg = await _service.AddPenaltyAsync(1, 1, new AddPenaltyRequest(3));

        reg.PenaltyPoints.Should().Be(5);
    }

    [Fact]
    public async Task AddPenaltyAsync_Negative_ThrowsArgumentException()
    {
        Func<Task> act = () => _service.AddPenaltyAsync(1, 1, new AddPenaltyRequest(-1));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddPenaltyAsync_NotRegistered_ThrowsRegistrationNotFoundException()
    {
        Func<Task> act = () => _service.AddPenaltyAsync(1, 9999, new AddPenaltyRequest(1));

        await act.Should().ThrowAsync<RegistrationNotFoundException>();
    }

    // ── Per-tournament isolation ───────────────────────────────

    [Fact]
    public async Task Disqualification_IsScopedToOneTournament()
    {
        var otherTournament = await NewTournamentAsync("Autre");
        await _service.RegisterAsync(otherTournament, 1);

        await _service.DisqualifyAsync(1, 1); // disqualify champion 1 in tournament 1 only

        (await _service.GetRegistrationAsync(1, 1)).IsDisqualified.Should().BeTrue();
        (await _service.GetRegistrationAsync(otherTournament, 1)).IsDisqualified
            .Should().BeFalse("a champion disqualified in one tournament stays active in another");
    }
}
