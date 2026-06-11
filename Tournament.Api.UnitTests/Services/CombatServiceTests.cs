using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

[Trait("Category", "Combat")]
[Trait("Layer", "Service")]
public class CombatServiceTests
{
    private readonly CombatService _service = new();

    private const int Knight = 1, Mage = 2, Cleric = 3, Rogue = 4, Berserker = 5;

    private const int SwordSlash = 1, Guard = 3, WarCry = 4, SecondWind = 5;
    private const int Fireball = 6;
    private const int Smite = 10, GreaterHeal = 13;
    private const int Backstab = 15, ExposeWeakness = 18;
    private const int Execute = 22, Brace = 23;

    private Task<CombatResponse> Start(CombatantSpec c1, CombatantSpec c2)
        => _service.StartCombatAsync(new CreateCombatRequest(c1, c2));

    private async Task<CombatResponse> Resolve(int combatId, int skill1, int skill2)
    {
        await _service.SubmitActionAsync(combatId, new SubmitActionRequest(1, skill1));
        return await _service.SubmitActionAsync(combatId, new SubmitActionRequest(2, skill2));
    }

    [Fact]
    public async Task StartCombatAsync_ValidChampions_CreatesInProgressCombatAtFullHp()
    {
        // Act
        var combat = await Start(new("Arthur", Knight, 1), new("Merlin", Mage, 1));

        // Assert
        combat.Id.Should().BeGreaterThan(0);
        combat.Status.Should().Be("IN_PROGRESS");
        combat.Turn.Should().Be(1);
        combat.WinnerSlot.Should().BeNull();
        combat.Champion1.MaxHp.Should().Be(110);
        combat.Champion1.CurrentHp.Should().Be(110);
        combat.Champion2.CurrentHp.Should().Be(110);
        combat.Log.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(1, 110)]
    [InlineData(5, 150)]
    [InlineData(10, 200)]
    public async Task StartCombatAsync_MaxHp_Is100Plus10PerLevel(int level, int expectedHp)
    {
        // Act
        var combat = await Start(new("Hero", Knight, level), new("Foe", Knight, 1));

        // Assert
        combat.Champion1.Level.Should().Be(level);
        combat.Champion1.MaxHp.Should().Be(expectedHp);
        combat.Champion1.CurrentHp.Should().Be(expectedHp);
    }

    [Fact]
    public async Task StartCombatAsync_UnknownClass_ThrowsClassNotFoundException()
    {
        // Act
        Func<Task> act = () => Start(new("Ghost", 999, 1), new("Foe", Knight, 1));

        // Assert
        await act.Should().ThrowAsync<ClassNotFoundException>()
                 .Where(e => e.ClassId == 999);
    }

    [Fact]
    public async Task StartCombatAsync_EmptyName_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = () => Start(new("", Knight, 1), new("Foe", Knight, 1));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StartCombatAsync_LevelBelowOne_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = () => Start(new("Hero", Knight, 0), new("Foe", Knight, 1));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetCombatAsync_ExistingCombat_ReturnsIt()
    {
        // Arrange
        var created = await Start(new("Arthur", Knight, 1), new("Merlin", Mage, 1));

        // Act
        var fetched = await _service.GetCombatAsync(created.Id);

        // Assert
        fetched.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetCombatAsync_UnknownCombat_ThrowsCombatNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.GetCombatAsync(999999);

        // Assert
        await act.Should().ThrowAsync<CombatNotFoundException>()
                 .Where(e => e.CombatId == 999999);
    }

    [Fact]
    public async Task GetAllCombatsAsync_ContainsCreatedCombat()
    {
        // Arrange
        var created = await Start(new("Arthur", Knight, 1), new("Merlin", Mage, 1));

        // Act
        var all = await _service.GetAllCombatsAsync();

        // Assert
        all.Should().Contain(c => c.Id == created.Id);
    }

    [Fact]
    public async Task SubmitActionAsync_OnlyOneChampion_WaitsWithoutResolving()
    {
        // Arrange
        var combat = await Start(new("Arthur", Knight, 1), new("Bedivere", Knight, 1));

        // Act
        var state = await _service.SubmitActionAsync(combat.Id, new SubmitActionRequest(1, SwordSlash));

        // Assert
        state.Turn.Should().Be(1, "the turn only resolves once both champions have acted");
        state.Champion1.HasSubmittedAction.Should().BeTrue();
        state.Champion2.HasSubmittedAction.Should().BeFalse();
        state.Champion2.CurrentHp.Should().Be(110, "no damage is dealt before both submit");
    }

    [Fact]
    public async Task SubmitActionAsync_BothAttack_DealsDamageAndAdvancesTurn()
    {
        // Arrange
        var combat = await Start(new("Arthur", Knight, 1), new("Bedivere", Knight, 1));

        // Act
        var state = await Resolve(combat.Id, SwordSlash, SwordSlash);

        // Assert
        state.Turn.Should().Be(2);
        state.Status.Should().Be("IN_PROGRESS");
        state.Champion1.CurrentHp.Should().Be(85);
        state.Champion2.CurrentHp.Should().Be(85);
        state.Champion1.HasSubmittedAction.Should().BeFalse("submissions reset after resolution");
    }

    [Fact]
    public async Task SubmitActionAsync_Defend_ReducesIncomingDamage()
    {
        // Arrange
        var combat = await Start(new("Arthur", Knight, 1), new("Bedivere", Knight, 1));

        // Act
        var state = await Resolve(combat.Id, Guard, SwordSlash);

        // Assert
        state.Champion1.CurrentHp.Should().Be(105);
        state.Champion2.CurrentHp.Should().Be(110, "the defender did not attack");
        state.Champion1.Effects.Should().BeEmpty("a 1-turn Guard expires at end of turn");
    }

    [Fact]
    public async Task SubmitActionAsync_Heal_RestoresHpButNeverAboveMax()
    {
        // Arrange
        var combat = await Start(new("Lyra", Cleric, 1), new("Ragnar", Berserker, 1));

        // Act
        var afterT1 = await Resolve(combat.Id, Smite,  20);
        // Assert
        afterT1.Champion1.CurrentHp.Should().Be(78);

        var afterT2 = await Resolve(combat.Id, GreaterHeal, Brace);

        afterT2.Champion1.CurrentHp.Should().Be(110);
        afterT2.Log.Should().Contain(e => e.Message.Contains("healing 32"));
    }

    [Fact]
    public async Task SubmitActionAsync_AttackBuff_IncreasesDamageDealt()
    {
        // Arrange
        var combat = await Start(new("Arthur", Knight, 1), new("Bedivere", Knight, 1));

        // Act
        var afterT1 = await Resolve(combat.Id, WarCry, SecondWind);
        // Assert
        afterT1.Champion1.Effects.Should().Contain(e => e.EffectType == "ATTACK_UP" && e.Magnitude == 10);
        afterT1.Champion1.Effects.Single(e => e.EffectType == "ATTACK_UP").RemainingTurns.Should().Be(2);

        var afterT2 = await Resolve(combat.Id, SwordSlash, SecondWind);
        afterT2.Champion2.CurrentHp.Should().Be(75);
    }

    [Fact]
    public async Task SubmitActionAsync_DefenseDebuff_IncreasesDamageTaken()
    {
        // Arrange
        var combat = await Start(new("Sly", Rogue, 1), new("Bedivere", Knight, 1));

        await Resolve(combat.Id, ExposeWeakness, SecondWind);

        // Act
        var afterT2 = await Resolve(combat.Id, Backstab, SecondWind);

        // Assert
        afterT2.Champion2.CurrentHp.Should().Be(65);
    }

    [Fact]
    public async Task SubmitActionAsync_DuplicateSubmissionSameTurn_ThrowsInvalidCombatActionException()
    {
        // Arrange
        var combat = await Start(new("Arthur", Knight, 1), new("Bedivere", Knight, 1));
        await _service.SubmitActionAsync(combat.Id, new SubmitActionRequest(1, SwordSlash));

        // Act
        Func<Task> act = () => _service.SubmitActionAsync(combat.Id, new SubmitActionRequest(1, Guard));

        // Assert
        await act.Should().ThrowAsync<InvalidCombatActionException>();
    }

    [Fact]
    public async Task SubmitActionAsync_SkillFromAnotherClass_ThrowsInvalidCombatActionException()
    {
        // Arrange
        var combat = await Start(new("Arthur", Knight, 1), new("Merlin", Mage, 1));

        // Act
        Func<Task> act = () => _service.SubmitActionAsync(combat.Id, new SubmitActionRequest(1, Fireball));

        // Assert
        await act.Should().ThrowAsync<InvalidCombatActionException>();
    }

    [Fact]
    public async Task SubmitActionAsync_UnknownSkill_ThrowsSkillNotFoundException()
    {
        // Arrange
        var combat = await Start(new("Arthur", Knight, 1), new("Bedivere", Knight, 1));

        // Act
        Func<Task> act = () => _service.SubmitActionAsync(combat.Id, new SubmitActionRequest(1, 9999));

        // Assert
        await act.Should().ThrowAsync<SkillNotFoundException>();
    }

    [Fact]
    public async Task SubmitActionAsync_InvalidSlot_ThrowsInvalidCombatActionException()
    {
        // Arrange
        var combat = await Start(new("Arthur", Knight, 1), new("Bedivere", Knight, 1));

        // Act
        Func<Task> act = () => _service.SubmitActionAsync(combat.Id, new SubmitActionRequest(3, SwordSlash));

        // Assert
        await act.Should().ThrowAsync<InvalidCombatActionException>();
    }

    [Fact]
    public async Task SubmitActionAsync_UnknownCombat_ThrowsCombatNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.SubmitActionAsync(999999, new SubmitActionRequest(1, SwordSlash));

        // Assert
        await act.Should().ThrowAsync<CombatNotFoundException>();
    }

    [Fact]
    public async Task SubmitActionAsync_HigherLevelStrikesFirst_AndCanKnockOutBeforeTheSlowerActs()
    {
        // Arrange
        var combat = await Start(new("Pip", Berserker, 1), new("Titan", Berserker, 5));

        await Resolve(combat.Id, Execute, Execute);
        await Resolve(combat.Id, Execute, Execute);
        // Act
        var final = await Resolve(combat.Id, Execute, Execute);

        // Assert
        final.Status.Should().Be("COMPLETED");
        final.WinnerSlot.Should().Be(2);
        final.Champion1.CurrentHp.Should().Be(0);
        final.Champion2.CurrentHp.Should().Be(70, "the downed champion never landed its third Execute");
        final.Log.Should().Contain(e => e.Message.Contains("is down and cannot act"));
        final.Log.Should().Contain(e => e.Message.Contains("wins"));
    }

    [Fact]
    public async Task SubmitActionAsync_OnCompletedCombat_ThrowsInvalidCombatActionException()
    {
        // Arrange
        var combat = await Start(new("Pip", Berserker, 1), new("Pop", Berserker, 1));
        await _service.ForfeitAsync(combat.Id, new ForfeitRequest(1));

        // Act
        Func<Task> act = () => _service.SubmitActionAsync(combat.Id, new SubmitActionRequest(2, Execute));

        // Assert
        await act.Should().ThrowAsync<InvalidCombatActionException>();
    }

    [Fact]
    public async Task ForfeitAsync_Slot1Concedes_Slot2Wins()
    {
        // Arrange
        var combat = await Start(new("Coward", Knight, 1), new("Brave", Knight, 1));

        // Act
        var result = await _service.ForfeitAsync(combat.Id, new ForfeitRequest(1));

        // Assert
        result.Status.Should().Be("COMPLETED");
        result.WinnerSlot.Should().Be(2);
        result.Log.Should().Contain(e => e.Message.Contains("forfeits"));
    }

    [Fact]
    public async Task ForfeitAsync_Slot2Concedes_Slot1Wins()
    {
        // Arrange
        var combat = await Start(new("Brave", Knight, 1), new("Coward", Knight, 1));

        // Act
        var result = await _service.ForfeitAsync(combat.Id, new ForfeitRequest(2));

        // Assert
        result.WinnerSlot.Should().Be(1);
    }

    [Fact]
    public async Task ForfeitAsync_InvalidSlot_ThrowsInvalidCombatActionException()
    {
        // Arrange
        var combat = await Start(new("A", Knight, 1), new("B", Knight, 1));

        // Act
        Func<Task> act = () => _service.ForfeitAsync(combat.Id, new ForfeitRequest(7));

        // Assert
        await act.Should().ThrowAsync<InvalidCombatActionException>();
    }

    [Fact]
    public async Task ForfeitAsync_AlreadyCompleted_ThrowsInvalidCombatActionException()
    {
        // Arrange
        var combat = await Start(new("A", Knight, 1), new("B", Knight, 1));
        await _service.ForfeitAsync(combat.Id, new ForfeitRequest(1));

        // Act
        Func<Task> act = () => _service.ForfeitAsync(combat.Id, new ForfeitRequest(2));

        // Assert
        await act.Should().ThrowAsync<InvalidCombatActionException>();
    }

    [Fact]
    public async Task ForfeitAsync_UnknownCombat_ThrowsCombatNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.ForfeitAsync(999999, new ForfeitRequest(1));

        // Assert
        await act.Should().ThrowAsync<CombatNotFoundException>();
    }
}
