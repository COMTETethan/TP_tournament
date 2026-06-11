using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

/// <summary>
/// End-to-end integration across the real services: create two champions, pair them in a duel,
/// fight the duel as a combat, and verify the result flows back into the tournament — the duel
/// gets an outcome and duration, and the score derives from it.
/// </summary>
public class DuelCombatIntegrationTests
{
    [Fact]
    public async Task FightingADuel_WritesOutcomeDurationAndScore()
    {
        // ── Arrange: real services sharing the same player & duel stores ───────────
        var players      = new PlayerService();
        var duels        = new DuelService();
        var combat       = new CombatService();
        var tournaments  = new TournamentService();
        var orchestrator = new DuelCombatService(duels, players, combat);
        var scores       = new ScoreService(players, duels, tournaments);

        // Two champions in tournament 1.
        var hero = await players.AddPlayerAsync(1, new CreatePlayerRequest("Arthur",  ClassId: 1 /* Knight */,    Level: 2));
        var foe  = await players.AddPlayerAsync(1, new CreatePlayerRequest("Mordred", ClassId: 5 /* Berserker */, Level: 1));

        // A duel pairing them.
        var duel = await duels.CreateDuelAsync(1, new CreateDuelRequest(hero.Id, foe.Id, DuelOrder: 1));

        // ── Act: start the combat from the duel and fight to the finish ────────────
        var state = await orchestrator.StartFromDuelAsync(duel.Id);
        state.CombatStatus.Should().Be("IN_PROGRESS");
        state.DuelOutcome.Should().BeNull();

        for (var i = 0; state.CombatStatus == "IN_PROGRESS" && i < 50; i++)
        {
            await orchestrator.SubmitActionAsync(duel.Id, new SubmitActionRequest(1, 1));   // Knight: Sword Slash
            state = await orchestrator.SubmitActionAsync(duel.Id, new SubmitActionRequest(2, 22)); // Berserker: Execute
        }

        // ── Assert: the duel now carries the outcome and a duration ────────────────
        state.CombatStatus.Should().Be("COMPLETED");
        state.WinnerPlayerId.Should().BeOneOf(hero.Id, foe.Id);
        state.DuelOutcome.Should().BeOneOf("PLAYER1_WIN", "PLAYER2_WIN");

        var storedDuel = await duels.GetDuelAsync(duel.Id);
        storedDuel.Outcome.Should().Be(state.DuelOutcome);
        storedDuel.DurationSeconds.Should().NotBeNull();

        // ── Assert: the score derives from the duel — winner +3, loser 0 ───────────
        var winnerId = state.WinnerPlayerId!.Value;
        var loserId  = winnerId == hero.Id ? foe.Id : hero.Id;

        (await scores.GetPlayerScoreAsync(winnerId)).FinalScore.Should().Be(3);
        (await scores.GetPlayerScoreAsync(loserId)).FinalScore.Should().Be(0);

        // ── Assert: the recorded replay is available for this duel ─────────────────
        var replay = await orchestrator.GetReplayByDuelAsync(duel.Id);
        replay.Status.Should().Be("COMPLETED");
        replay.Events.Last().Type.Should().Be("VICTORY");
    }
}
