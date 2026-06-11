using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

[Trait("Category", "Tournament")]
[Trait("Layer", "Integration")]
public class FullTournamentScenarioTests
{
    private const int KnightClass = 1, BerserkerClass = 5;
    private const int SwordSlash = 1, Execute = 22;

    [Fact]
    public async Task OneUsersChampions_PlayInTwoTournaments()
    {
        // Arrange
        var tournaments   = new TournamentService();
        var players       = new PlayerService();
        var duels         = new DuelService();
        var combat        = new CombatService();
        var registrations = new TournamentPlayerService(players);
        var orchestrator  = new DuelCombatService(duels, players, combat);

        var arthur  = await players.CreatePlayerAsync(userId: 1, new CreatePlayerRequest("Arthur",  KnightClass,    Level: 2));
        var mordred = await players.CreatePlayerAsync(userId: 1, new CreatePlayerRequest("Mordred", BerserkerClass, Level: 1));

        var tournamentA = await tournaments.CreateTournamentAsync(new CreateTournamentRequest("Coupe d'Été"));
        await registrations.RegisterAsync(tournamentA.Id, arthur.Id);
        await registrations.RegisterAsync(tournamentA.Id, mordred.Id);

        var duelA   = await duels.CreateDuelAsync(tournamentA.Id, new CreateDuelRequest(arthur.Id, mordred.Id, DuelOrder: 1));
        // Act
        var resultA = await FightToEnd(orchestrator, duelA.Id);

        // Assert
        resultA.CombatStatus.Should().Be("COMPLETED");
        resultA.DuelOutcome.Should().BeOneOf("PLAYER1_WIN", "PLAYER2_WIN");
        (await duels.GetDuelAsync(duelA.Id)).Outcome.Should().NotBeNull();

        var tournamentB = await tournaments.CreateTournamentAsync(new CreateTournamentRequest("Coupe d'Hiver"));
        tournamentB.Id.Should().NotBe(tournamentA.Id);
        await registrations.RegisterAsync(tournamentB.Id, arthur.Id);
        await registrations.RegisterAsync(tournamentB.Id, mordred.Id);

        var duelB   = await duels.CreateDuelAsync(tournamentB.Id, new CreateDuelRequest(arthur.Id, mordred.Id, DuelOrder: 1));
        var resultB = await FightToEnd(orchestrator, duelB.Id);

        resultB.CombatStatus.Should().Be("COMPLETED");
        resultB.DuelOutcome.Should().BeOneOf("PLAYER1_WIN", "PLAYER2_WIN");
        resultB.Combat.Champion1.Name.Should().Be("Arthur");
        resultB.Combat.Champion2.Name.Should().Be("Mordred");

        (await registrations.GetRegistrationAsync(tournamentA.Id, arthur.Id)).PlayerId.Should().Be(arthur.Id);
        (await registrations.GetRegistrationAsync(tournamentB.Id, arthur.Id)).PlayerId.Should().Be(arthur.Id);
        duelA.Id.Should().NotBe(duelB.Id);
    }

    private static async Task<DuelCombatResponse> FightToEnd(DuelCombatService orchestrator, int duelId)
    {
        var state = await orchestrator.StartFromDuelAsync(duelId);
        for (var i = 0; state.CombatStatus == "IN_PROGRESS" && i < 50; i++)
        {
            await orchestrator.SubmitActionAsync(duelId, new SubmitActionRequest(1, SwordSlash));
            state = await orchestrator.SubmitActionAsync(duelId, new SubmitActionRequest(2, Execute));
        }
        return state;
    }
}
