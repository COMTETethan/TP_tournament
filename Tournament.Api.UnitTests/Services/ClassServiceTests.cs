using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class ClassServiceTests
{
    private readonly ClassService _service = new();

    // ── GetAllClassesAsync ─────────────────────────────────────

    [Fact]
    public async Task GetAllClassesAsync_ReturnsTheFiveSeededClasses()
    {
        var result = (await _service.GetAllClassesAsync()).ToList();

        result.Should().HaveCount(5);
        result.Should().Contain(c => c.Name == "Knight");
        result.Should().Contain(c => c.Name == "Berserker");
    }

    [Fact]
    public async Task GetAllClassesAsync_ReportsSkillCountPerClass()
    {
        var result = (await _service.GetAllClassesAsync()).ToList();

        result.Single(c => c.Name == "Knight").SkillCount.Should().Be(5);
        result.Single(c => c.Name == "Mage").SkillCount.Should().Be(4);
        result.Single(c => c.Name == "Berserker").SkillCount.Should().Be(4);
    }

    // ── GetClassAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetClassAsync_ExistingId_ReturnsClass()
    {
        var result = await _service.GetClassAsync(1);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Knight");
        result.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetClassAsync_UnknownId_ThrowsClassNotFoundException()
    {
        Func<Task> act = () => _service.GetClassAsync(999);

        await act.Should().ThrowAsync<ClassNotFoundException>()
                 .Where(e => e.ClassId == 999);
    }

    // ── GetClassSkillsAsync ────────────────────────────────────

    [Fact]
    public async Task GetClassSkillsAsync_Knight_ReturnsItsFiveSkills()
    {
        var result = (await _service.GetClassSkillsAsync(1)).ToList();

        result.Should().HaveCount(5);
        result.Should().OnlyContain(s => s.ClassId == 1);
        result.Should().Contain(s => s.Name == "Sword Slash" && s.Category == "ATTACK");
    }

    [Fact]
    public async Task GetClassSkillsAsync_UnknownClass_ThrowsClassNotFoundException()
    {
        Func<Task> act = () => _service.GetClassSkillsAsync(999);

        await act.Should().ThrowAsync<ClassNotFoundException>();
    }

    // ── GetSkillAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetSkillAsync_ExistingId_ReturnsSkill()
    {
        var result = await _service.GetSkillAsync(1);

        result.Id.Should().Be(1);
        result.Name.Should().Be("Sword Slash");
        result.Category.Should().Be("ATTACK");
        result.Power.Should().Be(25);
        result.AuraEffect.Should().BeNull();
    }

    [Fact]
    public async Task GetSkillAsync_AuraSkill_CarriesEffect()
    {
        // Skill 4 = War Cry (Knight AURA, ATTACK_UP)
        var result = await _service.GetSkillAsync(4);

        result.Category.Should().Be("AURA");
        result.AuraEffect.Should().Be("ATTACK_UP");
        result.Duration.Should().Be(3);
    }

    [Fact]
    public async Task GetSkillAsync_UnknownId_ThrowsSkillNotFoundException()
    {
        Func<Task> act = () => _service.GetSkillAsync(9999);

        await act.Should().ThrowAsync<SkillNotFoundException>()
                 .Where(e => e.SkillId == 9999);
    }

    // ── Roster design invariants (0–3 per category, max 5 total) ──

    [Fact]
    public async Task EverySeededClass_RespectsSkillLimits()
    {
        var classes = await _service.GetAllClassesAsync();

        foreach (var klass in classes)
        {
            var skills = (await _service.GetClassSkillsAsync(klass.Id)).ToList();

            skills.Should().HaveCountLessThanOrEqualTo(5,
                $"class {klass.Name} must have at most 5 skills");

            foreach (var byCategory in skills.GroupBy(s => s.Category))
            {
                byCategory.Should().HaveCountLessThanOrEqualTo(3,
                    $"class {klass.Name} must have at most 3 {byCategory.Key} skills");
            }
        }
    }

    [Theory]
    [InlineData("ATTACK")]
    [InlineData("DEFEND")]
    [InlineData("HEAL")]
    [InlineData("AURA")]
    public async Task EverySkill_HasAValidCategory(string category)
    {
        var classes = await _service.GetAllClassesAsync();
        var allSkills = new List<SkillResponse>();
        foreach (var klass in classes)
            allSkills.AddRange(await _service.GetClassSkillsAsync(klass.Id));

        allSkills.Should().Contain(s => s.Category == category,
            $"the roster should contain at least one {category} skill");
        allSkills.Should().OnlyContain(s =>
            s.Category == "ATTACK" || s.Category == "DEFEND" ||
            s.Category == "HEAL"   || s.Category == "AURA");
    }

    [Fact]
    public async Task AuraSkillsHaveAnEffect_NonAuraSkillsDoNot()
    {
        var classes = await _service.GetAllClassesAsync();
        var allSkills = new List<SkillResponse>();
        foreach (var klass in classes)
            allSkills.AddRange(await _service.GetClassSkillsAsync(klass.Id));

        allSkills.Where(s => s.Category == "AURA")
                 .Should().OnlyContain(s => s.AuraEffect != null);
        allSkills.Where(s => s.Category != "AURA")
                 .Should().OnlyContain(s => s.AuraEffect == null);
    }
}
