using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

/// <summary>
/// Walks the full happy path the product needs: create a tournament, add two champions, pair them
/// in a duel and fight it — then create a SECOND tournament and reuse the very same players in a
/// new duel/fight. Proves the end-to-end flow works across two tournaments.
/// </summary>
public class FullTournamentScenarioTests
{
    private const int KnightClass = 1, BerserkerClass = 5;
    private const int SwordSlash = 1, Execute = 22;

    [Fact]
    public async Task CreateFightThenSecondTournament_ReusesSamePlayers()
    {
        // Real services sharing the same in-memory stores (as the Singletons do at runtime).
        var tournaments  = new TournamentService();
        var players      = new PlayerService();
        var duels        = new DuelService();
        var combat       = new CombatService();
        var orchestrator = new DuelCombatService(duels, players, combat);

        // ── 1. Create a tournament ─────────────────────────────────────────────────
        var tournamentA = await tournaments.CreateTournamentAsync(new CreateTournamentRequest("Coupe d'Été"));

        // ── 2. Add two champions to it ─────────────────────────────────────────────
        var arthur  = await players.AddPlayerAsync(tournamentA.Id, new CreatePlayerRequest("Arthur",  ClassId: KnightClass,    Level: 2));
        var mordred = await players.AddPlayerAsync(tournamentA.Id, new CreatePlayerRequest("Mordred", ClassId: BerserkerClass, Level: 1));

        // ── 3. Create a duel between them and fight it to the finish ───────────────
        var duelA   = await duels.CreateDuelAsync(tournamentA.Id, new CreateDuelRequest(arthur.Id, mordred.Id, DuelOrder: 1));
        var resultA = await FightToEnd(orchestrator, duelA.Id);

        resultA.CombatStatus.Should().Be("COMPLETED");
        resultA.DuelOutcome.Should().BeOneOf("PLAYER1_WIN", "PLAYER2_WIN");
        resultA.WinnerPlayerId.Should().BeOneOf(arthur.Id, mordred.Id);
        (await duels.GetDuelAsync(duelA.Id)).Outcome.Should().NotBeNull();

        // ── 4. Launch a second tournament ──────────────────────────────────────────
        var tournamentB = await tournaments.CreateTournamentAsync(new CreateTournamentRequest("Coupe d'Hiver"));
        tournamentB.Id.Should().NotBe(tournamentA.Id);

        // ── 5. Reuse the SAME players in a new duel in the second tournament ───────
        var duelB   = await duels.CreateDuelAsync(tournamentB.Id, new CreateDuelRequest(arthur.Id, mordred.Id, DuelOrder: 1));
        var resultB = await FightToEnd(orchestrator, duelB.Id);

        resultB.CombatStatus.Should().Be("COMPLETED");
        resultB.DuelOutcome.Should().BeOneOf("PLAYER1_WIN", "PLAYER2_WIN");
        resultB.WinnerPlayerId.Should().BeOneOf(arthur.Id, mordred.Id);

        // Two distinct duels, in two distinct tournaments, both decided, same two players.
        duelA.Id.Should().NotBe(duelB.Id);
        (await duels.GetDuelAsync(duelB.Id)).TournamentId.Should().Be(tournamentB.Id);
        (await duels.GetDuelAsync(duelB.Id)).Outcome.Should().NotBeNull();
        resultB.Combat.Champion1.Name.Should().Be("Arthur");
        resultB.Combat.Champion2.Name.Should().Be("Mordred");
    }

    private static async Task<DuelCombatResponse> FightToEnd(DuelCombatService orchestrator, int duelId)
    {
        var state = await orchestrator.StartFromDuelAsync(duelId);
        for (var i = 0; state.CombatStatus == "IN_PROGRESS" && i < 50; i++)
        {
            await orchestrator.SubmitActionAsync(duelId, new SubmitActionRequest(1, SwordSlash)); // Knight
            state = await orchestrator.SubmitActionAsync(duelId, new SubmitActionRequest(2, Execute)); // Berserker
        }
        return state;
    }
}
