using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Services;
using Xunit.Abstractions;

namespace Tournament.Api.UnitTests.Services;

/// <summary>
/// End-to-end smoke test: plays a whole combat through the public service (start → submit actions
/// every turn → automatic resolution → recorded replay) and prints the narrative, proving a duel
/// can be fought from beginning to end and is then available as a read-only replay.
/// Run with: dotnet test --filter FullyQualifiedName~CombatSimulation -l "console;verbosity=detailed"
/// </summary>
public class CombatSimulationTests
{
    private readonly ITestOutputHelper _out;
    private readonly CombatService _service = new();

    // Knight skills
    private const int SwordSlash = 1, Guard = 3, WarCry = 4, SecondWind = 5;
    // Cleric skills
    private const int Smite = 10, Sanctuary = 11, GreaterHeal = 13, Bless = 14;

    public CombatSimulationTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public async Task FullCombat_FromStartToRecordedReplay()
    {
        // ── Arrange: a Knight duels a Cleric ───────────────────────────────────────
        var combat = await _service.StartCombatAsync(new CreateCombatRequest(
            new CombatantSpec("Arthur", ClassId: 1 /* Knight */, Level: 2),
            new CombatantSpec("Lyra",   ClassId: 3 /* Cleric */, Level: 1)));

        _out.WriteLine($"=== Combat #{combat.Id} : " +
                       $"{combat.Champion1.Name} (Knight Lv2, {combat.Champion1.MaxHp} HP) vs " +
                       $"{combat.Champion2.Name} (Cleric Lv1, {combat.Champion2.MaxHp} HP) ===");

        // Deterministic scripts — each champion cycles through skills of every category.
        int[] knightPlan = { WarCry, SwordSlash, Guard, SwordSlash, SecondWind, SwordSlash, SwordSlash, SwordSlash, SwordSlash, SwordSlash };
        int[] clericPlan = { Bless,  Smite,      Smite, GreaterHeal, Smite,     Sanctuary,  Smite,      Smite,      Smite,      Smite };

        // ── Act: play turns until someone wins (safety cap so a bug can't hang) ─────
        var state = combat;
        for (var turn = 0; state.Status == "IN_PROGRESS" && turn < 50; turn++)
        {
            var knightSkill = knightPlan[Math.Min(turn, knightPlan.Length - 1)];
            var clericSkill = clericPlan[Math.Min(turn, clericPlan.Length - 1)];

            await _service.SubmitActionAsync(state.Id, new SubmitActionRequest(1, knightSkill));
            state = await _service.SubmitActionAsync(state.Id, new SubmitActionRequest(2, clericSkill));
        }

        // ── Assert: the duel finished with a winner ────────────────────────────────
        state.Status.Should().Be("COMPLETED");
        state.WinnerSlot.Should().BeOneOf(1, 2);

        // ── The backend recorded a replay; print it as the frontend would read it ──
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
