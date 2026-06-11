using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class ObjectiveServiceTests
{
    private readonly ObjectiveService _service = new();

    // ── CreateObjectiveAsync ───────────────────────────────────

    [Fact]
    public async Task CreateObjectiveAsync_ValidRequest_ReturnsObjective()
    {
        var request = new CreateObjectiveRequest(
            SeasonId: 1,
            Name: "Premier sang",
            Description: "Remporter votre premier duel",
            ObjectiveType: "WIN_DUELS",
            TargetValue: 1,
            XpReward: 100,
            ResetType: "NONE"
        );

        var result = await _service.CreateObjectiveAsync(request);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.SeasonId.Should().Be(1);
        result.Name.Should().Be("Premier sang");
        result.ObjectiveType.Should().Be("WIN_DUELS");
        result.TargetValue.Should().Be(1);
        result.XpReward.Should().Be(100);
        result.ResetType.Should().Be("NONE");
    }

    [Fact]
    public async Task CreateObjectiveAsync_EmptyName_ThrowsArgumentException()
    {
        var request = new CreateObjectiveRequest(1, "", "desc", "WIN_DUELS", 1, 100, "NONE");

        Func<Task> act = () => _service.CreateObjectiveAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateObjectiveAsync_ZeroXpReward_ThrowsArgumentException()
    {
        var request = new CreateObjectiveRequest(1, "Objectif", "desc", "WIN_DUELS", 1, 0, "NONE");

        Func<Task> act = () => _service.CreateObjectiveAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── GetObjectiveAsync ──────────────────────────────────────

    [Fact]
    public async Task GetObjectiveAsync_ExistingObjective_ReturnsObjective()
    {
        var created = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Objectif Test", "desc", "WIN_DUELS", 3, 200, "DAILY"));

        var result = await _service.GetObjectiveAsync(created.Id);

        result.Id.Should().Be(created.Id);
        result.Name.Should().Be("Objectif Test");
    }

    [Fact]
    public async Task GetObjectiveAsync_NonExistingObjective_ThrowsObjectiveNotFoundException()
    {
        Func<Task> act = () => _service.GetObjectiveAsync(9999);

        await act.Should().ThrowAsync<ObjectiveNotFoundException>()
                 .Where(e => e.ObjectiveId == 9999);
    }

    // ── GetSeasonObjectivesAsync ───────────────────────────────

    [Fact]
    public async Task GetSeasonObjectivesAsync_AfterCreatingTwo_ReturnsBoth()
    {
        await _service.CreateObjectiveAsync(new CreateObjectiveRequest(10, "Obj A", "d", "WIN_DUELS", 1, 100, "NONE"));
        await _service.CreateObjectiveAsync(new CreateObjectiveRequest(10, "Obj B", "d", "WIN_STREAK", 3, 300, "WEEKLY"));

        var result = await _service.GetSeasonObjectivesAsync(seasonId: 10);

        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Should().OnlyContain(o => o.SeasonId == 10);
    }

    // ── GetPlayerProgressAsync ─────────────────────────────────

    [Fact]
    public async Task GetPlayerProgressAsync_NewPlayer_ReturnsZeroProgress()
    {
        var obj = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Obj Progress", "d", "WIN_DUELS", 5, 500, "NONE"));

        var result = await _service.GetPlayerProgressAsync(obj.Id, playerId: 1);

        result.Should().NotBeNull();
        result.ObjectiveId.Should().Be(obj.Id);
        result.PlayerId.Should().Be(1);
        result.CurrentValue.Should().Be(0);
        result.IsCompleted.Should().BeFalse();
        result.CompletedAt.Should().BeNull();
    }

    // ── UpdatePlayerProgressAsync ──────────────────────────────

    [Fact]
    public async Task UpdatePlayerProgressAsync_BelowTarget_NotCompleted()
    {
        var obj = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Obj Update", "d", "WIN_DUELS", 5, 500, "NONE"));

        var result = await _service.UpdatePlayerProgressAsync(obj.Id, playerId: 1, new UpdateObjectiveProgressRequest(3));

        result.CurrentValue.Should().Be(3);
        result.IsCompleted.Should().BeFalse();
        result.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePlayerProgressAsync_ReachingTarget_MarkAsCompleted()
    {
        var obj = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Obj Complete", "d", "WIN_DUELS", 3, 300, "NONE"));

        var result = await _service.UpdatePlayerProgressAsync(obj.Id, playerId: 1, new UpdateObjectiveProgressRequest(3));

        result.IsCompleted.Should().BeTrue();
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdatePlayerProgressAsync_ExceedingTarget_CapsAtTarget()
    {
        var obj = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Obj Cap", "d", "WIN_DUELS", 3, 300, "NONE"));

        var result = await _service.UpdatePlayerProgressAsync(obj.Id, playerId: 1, new UpdateObjectiveProgressRequest(10));

        result.CurrentValue.Should().Be(3, "progress is capped at target value");
        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePlayerProgressAsync_NonExistingObjective_ThrowsObjectiveNotFoundException()
    {
        Func<Task> act = () => _service.UpdatePlayerProgressAsync(9999, 1, new UpdateObjectiveProgressRequest(1));

        await act.Should().ThrowAsync<ObjectiveNotFoundException>()
                 .Where(e => e.ObjectiveId == 9999);
    }

    // ── Period-aware completions (DAILY / WEEKLY) ──────────────

    [Fact]
    public async Task UpdatePlayerProgressAsync_DailyObjective_CompletionRecordHasPeriodKey()
    {
        var obj = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Duel du jour", "d", "WIN_DUELS", 1, 50, "DAILY"));

        await _service.UpdatePlayerProgressAsync(obj.Id, 1, new UpdateObjectiveProgressRequest(1, "2026-06-11"));

        var completions = (await _service.GetPlayerCompletionsAsync(obj.Id, 1)).ToList();
        completions.Should().HaveCount(1);
        completions[0].PeriodKey.Should().Be("2026-06-11");
        completions[0].XpAwarded.Should().Be(50);
    }

    [Fact]
    public async Task UpdatePlayerProgressAsync_SamePeriodTwice_DoesNotCreateSecondCompletion()
    {
        var obj = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Duel du jour", "d", "WIN_DUELS", 1, 50, "DAILY"));

        await _service.UpdatePlayerProgressAsync(obj.Id, 1, new UpdateObjectiveProgressRequest(1, "2026-06-11"));
        await _service.UpdatePlayerProgressAsync(obj.Id, 1, new UpdateObjectiveProgressRequest(1, "2026-06-11"));

        var completions = await _service.GetPlayerCompletionsAsync(obj.Id, 1);
        completions.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdatePlayerProgressAsync_DifferentPeriods_CreatesTwoCompletions()
    {
        var obj = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Duel du jour", "d", "WIN_DUELS", 1, 50, "DAILY"));

        await _service.UpdatePlayerProgressAsync(obj.Id, 1, new UpdateObjectiveProgressRequest(1, "2026-06-11"));
        await _service.UpdatePlayerProgressAsync(obj.Id, 1, new UpdateObjectiveProgressRequest(1, "2026-06-12"));

        var completions = (await _service.GetPlayerCompletionsAsync(obj.Id, 1)).ToList();
        completions.Should().HaveCount(2);
        completions.Select(c => c.PeriodKey).Should().Contain(["2026-06-11", "2026-06-12"]);
    }

    [Fact]
    public async Task UpdatePlayerProgressAsync_WeeklyObjective_CompletionHasWeekKey()
    {
        var obj = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Semaine glorieuse", "d", "WIN_DUELS", 5, 300, "WEEKLY"));

        await _service.UpdatePlayerProgressAsync(obj.Id, 1, new UpdateObjectiveProgressRequest(5, "2026-W24"));

        var completions = (await _service.GetPlayerCompletionsAsync(obj.Id, 1)).ToList();
        completions.Should().HaveCount(1);
        completions[0].PeriodKey.Should().Be("2026-W24");
    }

    [Fact]
    public async Task GetPlayerProgressAsync_WithPeriodKey_ReturnsPeriodSpecificProgress()
    {
        var obj = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Duel du jour", "d", "WIN_DUELS", 3, 50, "DAILY"));

        await _service.UpdatePlayerProgressAsync(obj.Id, 1, new UpdateObjectiveProgressRequest(2, "2026-06-11"));

        var progress = await _service.GetPlayerProgressAsync(obj.Id, 1, "2026-06-11");
        progress.CurrentValue.Should().Be(2);
        progress.PeriodKey.Should().Be("2026-06-11");
    }

    [Fact]
    public async Task GetPlayerProgressAsync_DifferentPeriods_ReturnIsolatedProgress()
    {
        var obj = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Duel du jour", "d", "WIN_DUELS", 3, 50, "DAILY"));

        await _service.UpdatePlayerProgressAsync(obj.Id, 1, new UpdateObjectiveProgressRequest(3, "2026-06-11"));

        var progressDay2 = await _service.GetPlayerProgressAsync(obj.Id, 1, "2026-06-12");
        progressDay2.CurrentValue.Should().Be(0, "new period starts from zero");
        progressDay2.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetPlayerCompletionsAsync_NonExistingObjective_ThrowsObjectiveNotFoundException()
    {
        Func<Task> act = () => _service.GetPlayerCompletionsAsync(9999, 1);

        await act.Should().ThrowAsync<ObjectiveNotFoundException>();
    }

    [Fact]
    public async Task GetPlayerProgressAsync_CalledTwice_ReturnsSameProgress()
    {
        var obj = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Obj Existant", "d", "WIN_DUELS", 5, 100, "NONE"));

        var first  = await _service.GetPlayerProgressAsync(obj.Id, 1);
        var second = await _service.GetPlayerProgressAsync(obj.Id, 1);

        second.Should().BeEquivalentTo(first);
    }

    [Fact]
    public async Task GetPlayerProgressAsync_TwoPlayers_EachGetsOwnProgress()
    {
        var obj = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Obj Multi", "d", "WIN_DUELS", 5, 100, "NONE"));

        await _service.GetPlayerProgressAsync(obj.Id, 1);
        var p2 = await _service.GetPlayerProgressAsync(obj.Id, 2);

        p2.PlayerId.Should().Be(2);
        p2.CurrentValue.Should().Be(0);
    }

    [Fact]
    public async Task GetPlayerProgressAsync_TwoObjectives_EachGetsOwnProgress()
    {
        var obj1 = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Obj A", "d", "WIN_DUELS", 5, 100, "NONE"));
        var obj2 = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Obj B", "d", "WIN_DUELS", 5, 100, "NONE"));

        await _service.GetPlayerProgressAsync(obj1.Id, 1);
        var p2 = await _service.GetPlayerProgressAsync(obj2.Id, 1);

        p2.ObjectiveId.Should().Be(obj2.Id);
        p2.CurrentValue.Should().Be(0);
    }

    [Fact]
    public async Task GetPlayerCompletionsAsync_CompletionExistsForOtherPlayer_ReturnsEmpty()
    {
        var obj = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Obj Completions", "d", "WIN_DUELS", 1, 100, "DAILY"));

        await _service.UpdatePlayerProgressAsync(obj.Id, 1, new UpdateObjectiveProgressRequest(1, "2026-06-11"));

        var result = await _service.GetPlayerCompletionsAsync(obj.Id, 2);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlayerCompletionsAsync_CompletionForDifferentObjective_ReturnsEmpty()
    {
        var obj1 = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Obj 1", "d", "WIN_DUELS", 1, 100, "DAILY"));
        var obj2 = await _service.CreateObjectiveAsync(
            new CreateObjectiveRequest(1, "Obj 2", "d", "WIN_DUELS", 1, 100, "DAILY"));

        await _service.UpdatePlayerProgressAsync(obj1.Id, 1, new UpdateObjectiveProgressRequest(1, "2026-06-11"));

        var result = await _service.GetPlayerCompletionsAsync(obj2.Id, 1);

        result.Should().BeEmpty();
    }
}
