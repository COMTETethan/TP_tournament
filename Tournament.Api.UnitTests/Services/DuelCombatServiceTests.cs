using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

/// <summary>
/// Unit tests for the duel↔combat orchestration, with the duel/player/combat services mocked.
/// Each test uses a unique duel id because the duel→combat link is a process-wide static map.
/// </summary>
public class DuelCombatServiceTests
{
    private readonly Mock<IDuelService>   _duels   = new();
    private readonly Mock<IPlayerService> _players = new();
    private readonly Mock<ICombatService> _combat  = new();
    private readonly DuelCombatService    _service;

    // Mutable so the SetDuelOutcome callback can evolve what GetDuelAsync returns.
    private DuelResponse _duel = new(0, 1, 1, 2, null, 1, DateTime.UtcNow, null);

    public DuelCombatServiceTests()
    {
        _service = new DuelCombatService(_duels.Object, _players.Object, _combat.Object);

        _duels.Setup(d => d.GetDuelAsync(It.IsAny<int>())).ReturnsAsync(() => _duel);
        _duels.Setup(d => d.SetDuelOutcomeAsync(It.IsAny<int>(), It.IsAny<SetDuelOutcomeRequest>()))
              .Callback<int, SetDuelOutcomeRequest>((_, req) => _duel = _duel with { Outcome = req.Outcome })
              .ReturnsAsync(() => _duel);
        _duels.Setup(d => d.EndDuelAsync(It.IsAny<int>(), It.IsAny<EndDuelRequest>())).ReturnsAsync(() => _duel);

        // Player 1 = Knight Lv2, Player 2 = Berserker Lv1.
        _players.Setup(p => p.GetPlayerAsync(1)).ReturnsAsync(new PlayerResponse(1, 1, "Arthur",  false, 0, 1, 2));
        _players.Setup(p => p.GetPlayerAsync(2)).ReturnsAsync(new PlayerResponse(2, 1, "Mordred", false, 0, 5, 1));
    }

    private static CombatResponse Combat(int id, string status, int? winner, int turn)
    {
        var c1 = new CombatantState(1, "Arthur",  1, 2, 120, 50, false, new List<ActiveEffectState>());
        var c2 = new CombatantState(2, "Mordred", 5, 1, 110, 50, false, new List<ActiveEffectState>());
        return new CombatResponse(id, status, turn, winner, c1, c2, new List<CombatLogEntry>(), DateTime.UtcNow);
    }

    private static CombatResponse InProgress(int id) => Combat(id, "IN_PROGRESS", null, 1);
    private static CombatResponse Completed(int id, int winner, int turn) => Combat(id, "COMPLETED", winner, turn);

    [Fact]
    public void DefaultConstructor_CreatesInstanceWithInMemoryServices()
    {
        var service = new DuelCombatService();
        service.Should().NotBeNull();
    }

    // ── StartFromDuelAsync ─────────────────────────────────────

    [Fact]
    public async Task StartFromDuelAsync_ValidDuel_StartsCombatWithPlayersAsChampions()
    {
        _duel = new(7001, 1, 1, 2, null, 1, DateTime.UtcNow, null);
        _combat.Setup(c => c.StartCombatAsync(It.IsAny<CreateCombatRequest>())).ReturnsAsync(InProgress(501));

        var result = await _service.StartFromDuelAsync(7001);

        result.DuelId.Should().Be(7001);
        result.CombatId.Should().Be(501);
        result.CombatStatus.Should().Be("IN_PROGRESS");
        result.DuelOutcome.Should().BeNull();
        _combat.Verify(c => c.StartCombatAsync(It.Is<CreateCombatRequest>(r =>
            r.Champion1.Name == "Arthur"  && r.Champion1.ClassId == 1 && r.Champion1.Level == 2 &&
            r.Champion2.Name == "Mordred" && r.Champion2.ClassId == 5 && r.Champion2.Level == 1)), Times.Once);
    }

    [Fact]
    public async Task StartFromDuelAsync_PlayerWithoutClass_ThrowsInvalidCombatActionException()
    {
        _duel = new(7002, 1, 1, 2, null, 1, DateTime.UtcNow, null);
        _players.Setup(p => p.GetPlayerAsync(1)).ReturnsAsync(new PlayerResponse(1, 1, "NoClass", false, 0, null, 1));

        Func<Task> act = () => _service.StartFromDuelAsync(7002);

        await act.Should().ThrowAsync<InvalidCombatActionException>();
    }

    [Fact]
    public async Task StartFromDuelAsync_DuelAlreadyDecided_ThrowsInvalidCombatActionException()
    {
        _duel = new(7003, 1, 1, 2, "PLAYER1_WIN", 1, DateTime.UtcNow, 120);

        Func<Task> act = () => _service.StartFromDuelAsync(7003);

        await act.Should().ThrowAsync<InvalidCombatActionException>();
    }

    [Fact]
    public async Task StartFromDuelAsync_UnknownDuel_ThrowsDuelNotFoundException()
    {
        _duels.Setup(d => d.GetDuelAsync(7004)).ThrowsAsync(new DuelNotFoundException(7004));

        Func<Task> act = () => _service.StartFromDuelAsync(7004);

        await act.Should().ThrowAsync<DuelNotFoundException>();
    }

    [Fact]
    public async Task StartFromDuelAsync_CombatAlreadyStarted_ThrowsInvalidCombatActionException()
    {
        _duel = new(7010, 1, 1, 2, null, 1, DateTime.UtcNow, null);
        _combat.Setup(c => c.StartCombatAsync(It.IsAny<CreateCombatRequest>())).ReturnsAsync(InProgress(510));
        await _service.StartFromDuelAsync(7010);

        Func<Task> act = () => _service.StartFromDuelAsync(7010);

        await act.Should().ThrowAsync<InvalidCombatActionException>();
    }

    // ── SubmitActionAsync / ForfeitAsync — outcome propagation ──

    [Fact]
    public async Task SubmitActionAsync_WhenCombatCompletes_WritesDuelOutcomeAndEndsDuel()
    {
        _duel = new(7005, 1, 1, 2, null, 1, DateTime.UtcNow, null);
        _combat.Setup(c => c.StartCombatAsync(It.IsAny<CreateCombatRequest>())).ReturnsAsync(InProgress(505));
        await _service.StartFromDuelAsync(7005);
        _combat.Setup(c => c.SubmitActionAsync(505, It.IsAny<SubmitActionRequest>()))
               .ReturnsAsync(Completed(505, winner: 1, turn: 6));

        var result = await _service.SubmitActionAsync(7005, new SubmitActionRequest(1, 1));

        result.CombatStatus.Should().Be("COMPLETED");
        result.WinnerSlot.Should().Be(1);
        result.WinnerPlayerId.Should().Be(1);
        result.DuelOutcome.Should().Be("PLAYER1_WIN");
        _duels.Verify(d => d.SetDuelOutcomeAsync(7005, It.Is<SetDuelOutcomeRequest>(r => r.Outcome == "PLAYER1_WIN")), Times.Once);
        _duels.Verify(d => d.EndDuelAsync(7005, It.IsAny<EndDuelRequest>()), Times.Once);
    }

    [Fact]
    public async Task SubmitActionAsync_WhileOngoing_DoesNotTouchTheDuel()
    {
        _duel = new(7006, 1, 1, 2, null, 1, DateTime.UtcNow, null);
        _combat.Setup(c => c.StartCombatAsync(It.IsAny<CreateCombatRequest>())).ReturnsAsync(InProgress(506));
        await _service.StartFromDuelAsync(7006);
        _combat.Setup(c => c.SubmitActionAsync(506, It.IsAny<SubmitActionRequest>())).ReturnsAsync(InProgress(506));

        var result = await _service.SubmitActionAsync(7006, new SubmitActionRequest(1, 1));

        result.CombatStatus.Should().Be("IN_PROGRESS");
        result.DuelOutcome.Should().BeNull();
        _duels.Verify(d => d.SetDuelOutcomeAsync(It.IsAny<int>(), It.IsAny<SetDuelOutcomeRequest>()), Times.Never);
    }

    [Fact]
    public async Task ForfeitAsync_WhenCombatCompletes_WritesOutcomeForTheOpponent()
    {
        _duel = new(7007, 1, 1, 2, null, 1, DateTime.UtcNow, null);
        _combat.Setup(c => c.StartCombatAsync(It.IsAny<CreateCombatRequest>())).ReturnsAsync(InProgress(507));
        await _service.StartFromDuelAsync(7007);
        _combat.Setup(c => c.ForfeitAsync(507, It.IsAny<ForfeitRequest>()))
               .ReturnsAsync(Completed(507, winner: 2, turn: 3));

        var result = await _service.ForfeitAsync(7007, new ForfeitRequest(1));

        result.WinnerSlot.Should().Be(2);
        result.WinnerPlayerId.Should().Be(2);
        result.DuelOutcome.Should().Be("PLAYER2_WIN");
        _duels.Verify(d => d.SetDuelOutcomeAsync(7007, It.Is<SetDuelOutcomeRequest>(r => r.Outcome == "PLAYER2_WIN")), Times.Once);
    }

    [Fact]
    public async Task SubmitActionAsync_NoCombatStartedForDuel_ThrowsInvalidCombatActionException()
    {
        _duel = new(7008, 1, 1, 2, null, 1, DateTime.UtcNow, null);

        Func<Task> act = () => _service.SubmitActionAsync(7008, new SubmitActionRequest(1, 1));

        await act.Should().ThrowAsync<InvalidCombatActionException>();
    }

    // ── GetReplayByDuelAsync ───────────────────────────────────

    [Fact]
    public async Task GetReplayByDuelAsync_DelegatesToCombatReplay()
    {
        _duel = new(7009, 1, 1, 2, null, 1, DateTime.UtcNow, null);
        _combat.Setup(c => c.StartCombatAsync(It.IsAny<CreateCombatRequest>())).ReturnsAsync(InProgress(509));
        await _service.StartFromDuelAsync(7009);
        var replay = new CombatReplayResponse(509, "IN_PROGRESS", null, 1,
            new ReplayChampion(1, "Arthur", 1, "Knight", 2, 120),
            new ReplayChampion(2, "Mordred", 5, "Berserker", 1, 110),
            DateTime.UtcNow, null, new List<CombatEventResponse>());
        _combat.Setup(c => c.GetReplayAsync(509)).ReturnsAsync(replay);

        var result = await _service.GetReplayByDuelAsync(7009);

        result.CombatId.Should().Be(509);
    }
}
