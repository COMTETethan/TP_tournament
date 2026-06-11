using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

[Trait("Category", "DuelCombat")]
[Trait("Layer", "Integration")]
public class DuelCombatIntegrationTests
{
    [Fact]
    public async Task FightingADuel_WritesOutcomeDurationAndScore()
    {
        // Arrange
        var players       = new PlayerService();
        var duels         = new DuelService();
        var combat        = new CombatService();
        var tournaments   = new TournamentService();
        var registrations = new TournamentPlayerService(players);
        var orchestrator  = new DuelCombatService(duels, players, combat);
        var scores        = new ScoreService(registrations, duels, tournaments);

        var hero = await players.CreatePlayerAsync(1, new CreatePlayerRequest("Arthur",  ClassId: 1 ,    Level: 2));
        var foe  = await players.CreatePlayerAsync(1, new CreatePlayerRequest("Mordred", ClassId: 5 , Level: 1));
        await registrations.RegisterAsync(1, hero.Id);
        await registrations.RegisterAsync(1, foe.Id);

        var duel = await duels.CreateDuelAsync(1, new CreateDuelRequest(hero.Id, foe.Id, DuelOrder: 1));

        // Act
        var state = await orchestrator.StartFromDuelAsync(duel.Id);
        // Assert
        state.CombatStatus.Should().Be("IN_PROGRESS");
        state.DuelOutcome.Should().BeNull();

        for (var i = 0; state.CombatStatus == "IN_PROGRESS" && i < 50; i++)
        {
            await orchestrator.SubmitActionAsync(duel.Id, new SubmitActionRequest(1, 1));
            state = await orchestrator.SubmitActionAsync(duel.Id, new SubmitActionRequest(2, 22));
        }

        state.CombatStatus.Should().Be("COMPLETED");
        state.WinnerPlayerId.Should().BeOneOf(hero.Id, foe.Id);
        state.DuelOutcome.Should().BeOneOf("PLAYER1_WIN", "PLAYER2_WIN");

        var storedDuel = await duels.GetDuelAsync(duel.Id);
        storedDuel.Outcome.Should().Be(state.DuelOutcome);
        storedDuel.DurationSeconds.Should().NotBeNull();

        var winnerId = state.WinnerPlayerId!.Value;
        var loserId  = winnerId == hero.Id ? foe.Id : hero.Id;

        (await scores.GetPlayerScoreAsync(1, winnerId)).FinalScore.Should().Be(3);
        (await scores.GetPlayerScoreAsync(1, loserId)).FinalScore.Should().Be(0);

        var replay = await orchestrator.GetReplayByDuelAsync(duel.Id);
        replay.Status.Should().Be("COMPLETED");
        replay.Events.Last().Type.Should().Be("VICTORY");
    }
}
