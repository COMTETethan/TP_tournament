using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

[Trait("Category", "Combat")]
[Trait("Layer", "Service")]
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

    [Fact]
    public async Task GetReplayAsync_AfterKnockOut_RecordsCompleteOrderedStream()
    {
        // Arrange
        var combat = await Start(new("Pip", Berserker, 1), new("Titan", Berserker, 5));
        await Resolve(combat.Id, Execute, Execute);
        await Resolve(combat.Id, Execute, Execute);
        await Resolve(combat.Id, Execute, Execute);

        // Act
        var replay = await _service.GetReplayAsync(combat.Id);

        // Assert
        replay.Status.Should().Be("COMPLETED");
        replay.WinnerSlot.Should().Be(2);
        replay.CompletedAt.Should().NotBeNull();
        replay.TurnCount.Should().Be(3);

        replay.Events.Should().NotBeEmpty();
        replay.Events.First().Type.Should().Be("COMBAT_START");
        replay.Events.Last().Type.Should().Be("VICTORY");
        replay.Events.Should().Contain(e => e.Type == "SKIPPED", "the downed champion is recorded as unable to act");

        replay.Events.Last().Champion1Hp.Should().Be(0);
        replay.Events.Last().Champion2Hp.Should().Be(70);
    }

    [Fact]
    public async Task GetReplayAsync_Events_AreSequentiallyNumberedFromOne()
    {
        // Arrange
        var combat = await Start(new("Pip", Berserker, 1), new("Titan", Berserker, 5));
        await Resolve(combat.Id, Execute, Execute);
        await Resolve(combat.Id, Execute, Execute);
        await Resolve(combat.Id, Execute, Execute);

        var replay = await _service.GetReplayAsync(combat.Id);

        // Act
        replay.Events.Select(e => e.Sequence)
              // Assert
              .Should().BeInAscendingOrder()
              .And.Equal(Enumerable.Range(1, replay.Events.Count));
    }

    [Fact]
    public async Task GetReplayAsync_AttackEvents_CarrySkillTargetAndDamage()
    {
        // Arrange
        var combat = await Start(new("Pip", Berserker, 1), new("Titan", Berserker, 5));
        await Resolve(combat.Id, Execute, Execute);

        var replay = await _service.GetReplayAsync(combat.Id);

        // Act
        var attack = replay.Events.First(e => e.Type == "ATTACK");
        // Assert
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
        // Arrange
        var combat = await Start(new("Arthur", Knight, 2), new("Lyra", Cleric, 3));

        // Act
        var replay = await _service.GetReplayAsync(combat.Id);

        // Assert
        replay.Champion1.ClassName.Should().Be("Knight");
        replay.Champion1.Level.Should().Be(2);
        replay.Champion1.MaxHp.Should().Be(120);
        replay.Champion2.ClassName.Should().Be("Cleric");
    }

    [Fact]
    public async Task GetReplayAsync_RecordsHealAndAuraEvents()
    {
        // Arrange
        var combat = await Start(new("Arthur", Knight, 1), new("Lyra", Cleric, 1));

        await Resolve(combat.Id, WarCry, Smite);

        var replay = await _service.GetReplayAsync(combat.Id);

        // Act
        var aura = replay.Events.First(e => e.Type == "AURA");
        // Assert
        aura.Effect.Should().Be("ATTACK_UP");
        aura.Category.Should().Be("AURA");
        aura.ActorSlot.Should().Be(1);
    }

    [Fact]
    public async Task GetReplayAsync_RecordsHealAmount()
    {
        // Arrange
        var combat = await Start(new("Lyra", Cleric, 1), new("Ragnar", Berserker, 1));

        await Resolve(combat.Id, Smite, RecklessSwing);
        await Resolve(combat.Id, GreaterHeal, Brace);

        // Act
        var replay = await _service.GetReplayAsync(combat.Id);

        // Assert
        replay.Events.Should().Contain(e => e.Type == "HEAL" && e.Amount == 32);
    }

    [Fact]
    public async Task GetReplayAsync_AfterForfeit_RecordsForfeitAndCompletion()
    {
        // Arrange
        var combat = await Start(new("Coward", Knight, 1), new("Brave", Knight, 1));
        await _service.ForfeitAsync(combat.Id, new ForfeitRequest(1));

        // Act
        var replay = await _service.GetReplayAsync(combat.Id);

        // Assert
        replay.Status.Should().Be("COMPLETED");
        replay.WinnerSlot.Should().Be(2);
        replay.CompletedAt.Should().NotBeNull();
        replay.Events.Last().Type.Should().Be("FORFEIT");
    }

    [Fact]
    public async Task GetReplayAsync_OngoingCombat_IsNotYetCompleted()
    {
        // Arrange
        var combat = await Start(new("Arthur", Knight, 1), new("Bedivere", Knight, 1));
        await _service.SubmitActionAsync(combat.Id, new SubmitActionRequest(1, SecondWind));

        // Act
        var replay = await _service.GetReplayAsync(combat.Id);

        // Assert
        replay.Status.Should().Be("IN_PROGRESS");
        replay.CompletedAt.Should().BeNull();
        replay.WinnerSlot.Should().BeNull();
        replay.Events.Should().Contain(e => e.Type == "COMBAT_START");
        replay.Events.Should().NotContain(e => e.Type == "VICTORY" || e.Type == "FORFEIT");
    }

    [Fact]
    public async Task GetReplayAsync_UnknownCombat_ThrowsCombatNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.GetReplayAsync(999999);

        // Assert
        await act.Should().ThrowAsync<CombatNotFoundException>()
                 .Where(e => e.CombatId == 999999);
    }
}
