using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.UnitTests.Controllers;

[Trait("Category", "Dtos")]
[Trait("Layer", "Unit")]
public class ResponseRecordTests
{
    private static readonly DateTime Now = new(2026, 6, 11, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PlayerObjectiveCompletionResponse_EqualInstances_AreEqual()
    {
        // Arrange
        var r1 = new PlayerObjectiveCompletionResponse(1, 1, 1, Now, 100, "2026-06-11");
        // Act
        var r2 = new PlayerObjectiveCompletionResponse(1, 1, 1, Now, 100, "2026-06-11");

        // Assert
        (r1 == r2).Should().BeTrue();
        r1.GetHashCode().Should().Be(r2.GetHashCode());
    }

    [Fact]
    public void PlayerObjectiveCompletionResponse_DifferentInstances_AreNotEqual()
    {
        // Arrange
        var r1 = new PlayerObjectiveCompletionResponse(1, 1, 1, Now, 100, "2026-06-11");
        // Act
        var r2 = new PlayerObjectiveCompletionResponse(2, 2, 2, Now, 200, null);

        // Assert
        (r1 == r2).Should().BeFalse();
        r1.Equals(r2).Should().BeFalse();
    }

    [Fact]
    public void PlayerObjectiveCompletionResponse_ToString_ContainsId()
    {
        // Act
        var r = new PlayerObjectiveCompletionResponse(42, 1, 1, Now, 100, null);

        // Assert
        r.ToString().Should().Contain("42");
    }

    [Fact]
    public void PlayerSeasonRewardResponse_EqualInstances_AreEqual()
    {
        // Arrange
        var r1 = new PlayerSeasonRewardResponse(1, 1, 1, 1, 1, Now);
        // Act
        var r2 = new PlayerSeasonRewardResponse(1, 1, 1, 1, 1, Now);

        // Assert
        (r1 == r2).Should().BeTrue();
        r1.GetHashCode().Should().Be(r2.GetHashCode());
    }

    [Fact]
    public void PlayerSeasonRewardResponse_DifferentInstances_AreNotEqual()
    {
        // Arrange
        var r1 = new PlayerSeasonRewardResponse(1, 1, 1, 1, 1, Now);
        // Act
        var r2 = new PlayerSeasonRewardResponse(2, 2, 2, 2, 2, Now);

        // Assert
        (r1 == r2).Should().BeFalse();
        r1.Equals(r2).Should().BeFalse();
    }

    [Fact]
    public void PlayerSeasonRewardResponse_ToString_ContainsId()
    {
        // Act
        var r = new PlayerSeasonRewardResponse(99, 1, 1, 1, 1, Now);

        // Assert
        r.ToString().Should().Contain("99");
    }
}
