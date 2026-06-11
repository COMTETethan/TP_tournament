using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Services;
using Xunit.Abstractions;

namespace Tournament.Api.UnitTests.Services;

[Trait("Category", "Combat")]
[Trait("Layer", "Integration")]
public class CombatSimulationTests
{
    private readonly ITestOutputHelper _out;
    private readonly CombatService _service = new();

    private const int SwordSlash = 1, Guard = 3, WarCry = 4, SecondWind = 5;
    private const int Smite = 10, Sanctuary = 11, GreaterHeal = 13, Bless = 14;

    public CombatSimulationTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public async Task FullCombat_FromStartToRecordedReplay()
    {
        // Arrange
        var combat = await _service.StartCombatAsync(new CreateCombatRequest(
            new CombatantSpec("Arthur", ClassId: 1 , Level: 2),
            new CombatantSpec("Lyra",   ClassId: 3 , Level: 1)));

        _out.WriteLine($"=== Combat #{combat.Id} : " +
                       $"{combat.Champion1.Name} (Knight Lv2, {combat.Champion1.MaxHp} HP) vs " +
                       $"{combat.Champion2.Name} (Cleric Lv1, {combat.Champion2.MaxHp} HP) ===");

        int[] knightPlan = { WarCry, SwordSlash, Guard, SwordSlash, SecondWind, SwordSlash, SwordSlash, SwordSlash, SwordSlash, SwordSlash };
        int[] clericPlan = { Bless,  Smite,      Smite, GreaterHeal, Smite,     Sanctuary,  Smite,      Smite,      Smite,      Smite };

        var state = combat;
        for (var turn = 0; state.Status == "IN_PROGRESS" && turn < 50; turn++)
        {
            var knightSkill = knightPlan[Math.Min(turn, knightPlan.Length - 1)];
            var clericSkill = clericPlan[Math.Min(turn, clericPlan.Length - 1)];

            await _service.SubmitActionAsync(state.Id, new SubmitActionRequest(1, knightSkill));
            state = await _service.SubmitActionAsync(state.Id, new SubmitActionRequest(2, clericSkill));
        // Act
        }

        // Assert
        state.Status.Should().Be("COMPLETED");
        state.WinnerSlot.Should().BeOneOf(1, 2);

        var replay = await _service.GetReplayAsync(combat.Id);

        replay.Status.Should().Be("COMPLETED");
        replay.CompletedAt.Should().NotBeNull();
        replay.Events.First().Type.Should().Be("COMBAT_START");
        replay.Events.Last().Type.Should().Be("VICTORY");

        _out.WriteLine("");
        _out.WriteLine("--- RECORDED REPLAY (read-only, served to the frontend) ---");
        foreach (var e in replay.Events)
        {
            _out.WriteLine(
                $"[{e.Sequence,2}] T{e.Turn} {e.Type,-12} " +
                $"{replay.Champion1.Name}:{e.Champion1Hp,3}  {replay.Champion2.Name}:{e.Champion2Hp,3}  | {e.Message}");
        }

        var winner = replay.WinnerSlot == 1 ? replay.Champion1 : replay.Champion2;
        _out.WriteLine("");
        _out.WriteLine($">>> Winner: {winner.Name} ({winner.ClassName}) after {replay.TurnCount} turns, " +
                       $"{replay.Events.Count} recorded events.");
    }
}
