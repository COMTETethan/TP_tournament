using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.UnitTests.Controllers;

/// <summary>
/// Teste les membres synthétisés des records (Equals, GetHashCode, ToString)
/// pour couvrir les branches générées par le compilateur.
/// </summary>
public class ResponseRecordTests
{
    private static readonly DateTime Now = new(2026, 6, 11, 12, 0, 0, DateTimeKind.Utc);

    // ── PlayerObjectiveCompletionResponse ─────────────────────────────────────

    [Fact]
    public void PlayerObjectiveCompletionResponse_EqualInstances_AreEqual()
    {
        var r1 = new PlayerObjectiveCompletionResponse(1, 1, 1, Now, 100, "2026-06-11");
        var r2 = new PlayerObjectiveCompletionResponse(1, 1, 1, Now, 100, "2026-06-11");

        (r1 == r2).Should().BeTrue();
        r1.GetHashCode().Should().Be(r2.GetHashCode());
    }

    [Fact]
    public void PlayerObjectiveCompletionResponse_DifferentInstances_AreNotEqual()
    {
        var r1 = new PlayerObjectiveCompletionResponse(1, 1, 1, Now, 100, "2026-06-11");
        var r2 = new PlayerObjectiveCompletionResponse(2, 2, 2, Now, 200, null);

        (r1 == r2).Should().BeFalse();
        r1.Equals(r2).Should().BeFalse();
    }

    [Fact]
    public void PlayerObjectiveCompletionResponse_ToString_ContainsId()
    {
        var r = new PlayerObjectiveCompletionResponse(42, 1, 1, Now, 100, null);

        r.ToString().Should().Contain("42");
    }

    // ── PlayerSeasonRewardResponse ────────────────────────────────────────────

    [Fact]
    public void PlayerSeasonRewardResponse_EqualInstances_AreEqual()
    {
        var r1 = new PlayerSeasonRewardResponse(1, 1, 1, 1, 1, Now);
        var r2 = new PlayerSeasonRewardResponse(1, 1, 1, 1, 1, Now);

        (r1 == r2).Should().BeTrue();
        r1.GetHashCode().Should().Be(r2.GetHashCode());
    }

    [Fact]
    public void PlayerSeasonRewardResponse_DifferentInstances_AreNotEqual()
    {
        var r1 = new PlayerSeasonRewardResponse(1, 1, 1, 1, 1, Now);
        var r2 = new PlayerSeasonRewardResponse(2, 2, 2, 2, 2, Now);

        (r1 == r2).Should().BeFalse();
        r1.Equals(r2).Should().BeFalse();
    }

    [Fact]
    public void PlayerSeasonRewardResponse_ToString_ContainsId()
    {
        var r = new PlayerSeasonRewardResponse(99, 1, 1, 1, 1, Now);

        r.ToString().Should().Contain("99");
    }
}
