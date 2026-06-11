using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

/// <summary>
/// Verifies that every combat records a read-only replay (ordered event stream) that the backend
/// builds itself — the frontend only ever reads it back to watch an old fight.
/// </summary>
public class CombatReplayTests
{
    private readonly CombatService _service = new();

    private const int Knight = 1, Cleric = 3, Berserker = 5;
    private const int WarCry = 4, SecondWind = 5;
    private const int Smite = 10, GreaterHeal = 13;
    private const int RecklessSwing = 20, Execute = 22, Brace = 23;

    private Task<CombatResponse> Start(CombatantSpec c1, CombatantSpec c2)
        => _service.StartCombatAsync(new CreateCombatRequest(c1, c2));

    private async Task<CombatResponse> Resolve(int id, int skill1, int skill2)
    {
        await _service.SubmitActionAsync(id, new SubmitActionRequest(1, skill1));
        return await _service.SubmitActionAsync(id, new SubmitActionRequest(2, skill2));
    }

    // ── Recording a finished fight ─────────────────────────────

    [Fact]
    public async Task GetReplayAsync_AfterKnockOut_RecordsCompleteOrderedStream()
    {
        // Faster Titan (Lv5) KOs Pip (Lv1) on turn 3; both Execute every turn.
        var combat = await Start(new("Pip", Berserker, 1), new("Titan", Berserker, 5));
        await Resolve(combat.Id, Execute, Execute);
        await Resolve(combat.Id, Execute, Execute);
        await Resolve(combat.Id, Execute, Execute);

        var replay = await _service.GetReplayAsync(combat.Id);

        replay.Status.Should().Be("COMPLETED");
        replay.WinnerSlot.Should().Be(2);
        replay.CompletedAt.Should().NotBeNull();
        replay.TurnCount.Should().Be(3);

        replay.Events.Should().NotBeEmpty();
        replay.Events.First().Type.Should().Be("COMBAT_START");
        replay.Events.Last().Type.Should().Be("VICTORY");
        replay.Events.Should().Contain(e => e.Type == "SKIPPED", "the downed champion is recorded as unable to act");

        // The recorded HP snapshot on the final event matches the final state.
        replay.Events.Last().Champion1Hp.Should().Be(0);
        replay.Events.Last().Champion2Hp.Should().Be(70);
    }

    [Fact]
    public async Task GetReplayAsync_Events_AreSequentiallyNumberedFromOne()
    {
        var combat = await Start(new("Pip", Berserker, 1), new("Titan", Berserker, 5));
        await Resolve(combat.Id, Execute, Execute);
        await Resolve(combat.Id, Execute, Execute);
        await Resolve(combat.Id, Execute, Execute);

        var replay = await _service.GetReplayAsync(combat.Id);

        replay.Events.Select(e => e.Sequence)
              .Should().BeInAscendingOrder()
              .And.Equal(Enumerable.Range(1, replay.Events.Count));
    }

    [Fact]
    public async Task GetReplayAsync_AttackEvents_CarrySkillTargetAndDamage()
    {
        var combat = await Start(new("Pip", Berserker, 1), new("Titan", Berserker, 5));
        await Resolve(combat.Id, Execute, Execute);

        var replay = await _service.GetReplayAsync(combat.Id);

        var attack = replay.Events.First(e => e.Type == "ATTACK");
        attack.SkillId.Should().Be(Execute);
        attack.SkillName.Should().Be("Execute");
        attack.Category.Should().Be("ATTACK");
        attack.ActorSlot.Should().BeOneOf(1, 2);
        attack.TargetSlot.Should().BeOneOf(1, 2);
        attack.Amount.Should().Be(40);
    }

    [Fact]
    public async Task GetReplayAsync_ResolvesChampionClassNames()
    {
        var combat = await Start(new("Arthur", Knight, 2), new("Lyra", Cleric, 3));

        var replay = await _service.GetReplayAsync(combat.Id);

        replay.Champion1.ClassName.Should().Be("Knight");
        replay.Champion1.Level.Should().Be(2);
        replay.Champion1.MaxHp.Should().Be(120);
        replay.Champion2.ClassName.Should().Be("Cleric");
    }

    [Fact]
    public async Task GetReplayAsync_RecordsHealAndAuraEvents()
    {
        var combat = await Start(new("Arthur", Knight, 1), new("Lyra", Cleric, 1));

        // Turn 1: Knight War Cry (AURA ATTACK_UP); Cleric Smite (attack, no effect on this assert).
        await Resolve(combat.Id, WarCry, Smite);

        var replay = await _service.GetReplayAsync(combat.Id);

        var aura = replay.Events.First(e => e.Type == "AURA");
        aura.Effect.Should().Be("ATTACK_UP");
        aura.Category.Should().Be("AURA");
        aura.ActorSlot.Should().Be(1);
    }

    [Fact]
    public async Task GetReplayAsync_RecordsHealAmount()
    {
        var combat = await Start(new("Lyra", Cleric, 1), new("Ragnar", Berserker, 1));

        // Turn 1: Cleric takes 32 from Reckless Swing (Smite vs Reckless).
        await Resolve(combat.Id, Smite, RecklessSwing);
        // Turn 2: Greater Heal (45) capped to the 32 missing HP.
        await Resolve(combat.Id, GreaterHeal, Brace);

        var replay = await _service.GetReplayAsync(combat.Id);

        replay.Events.Should().Contain(e => e.Type == "HEAL" && e.Amount == 32);
    }

    // ── Recording a forfeit ────────────────────────────────────

    [Fact]
    public async Task GetReplayAsync_AfterForfeit_RecordsForfeitAndCompletion()
    {
        var combat = await Start(new("Coward", Knight, 1), new("Brave", Knight, 1));
        await _service.ForfeitAsync(combat.Id, new ForfeitRequest(1));

        var replay = await _service.GetReplayAsync(combat.Id);

        replay.Status.Should().Be("COMPLETED");
        replay.WinnerSlot.Should().Be(2);
        replay.CompletedAt.Should().NotBeNull();
        replay.Events.Last().Type.Should().Be("FORFEIT");
    }

    // ── Ongoing fight ──────────────────────────────────────────

    [Fact]
    public async Task GetReplayAsync_OngoingCombat_IsNotYetCompleted()
    {
        var combat = await Start(new("Arthur", Knight, 1), new("Bedivere", Knight, 1));
        await _service.SubmitActionAsync(combat.Id, new SubmitActionRequest(1, SecondWind));

        var replay = await _service.GetReplayAsync(combat.Id);

        replay.Status.Should().Be("IN_PROGRESS");
        replay.CompletedAt.Should().BeNull();
        replay.WinnerSlot.Should().BeNull();
        replay.Events.Should().Contain(e => e.Type == "COMBAT_START");
        replay.Events.Should().NotContain(e => e.Type == "VICTORY" || e.Type == "FORFEIT");
    }

    [Fact]
    public async Task GetReplayAsync_UnknownCombat_ThrowsCombatNotFoundException()
    {
        Func<Task> act = () => _service.GetReplayAsync(999999);

        await act.Should().ThrowAsync<CombatNotFoundException>()
                 .Where(e => e.CombatId == 999999);
    }
}
