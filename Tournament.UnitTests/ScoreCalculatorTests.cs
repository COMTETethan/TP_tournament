using FluentAssertions;
using Tournament.Domain.Service;

namespace Tournament.UnitTests;

[Trait("Category", "ScoreCalculator")]
[Trait("Layer", "Domain")]
public class ScoreCalculatorTests
{
    private readonly ScoreCalculator _calculator = new();

    private static MatchResult W() => new(MatchResult.Result.Win);
    private static MatchResult D() => new(MatchResult.Result.Draw);
    private static MatchResult L() => new(MatchResult.Result.Loss);

    [Fact]
    [Trait("Requirement", "REQ-T-001")]
    [Trait("Requirement", "REQ-T-002")]
    [Trait("Requirement", "REQ-T-003")]
    public void CalculateScore_WinDrawLoss_ReturnsFour()
    {
        // Arrange
        var matches = new List<MatchResult> { W(), D(), L() };

        // Act
        var score = _calculator.CalculateScore(matches);

        // Assert
        score.Should().Be(4, "because 3+1+0 = 4 points");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-001")]
    public void CalculateScore_TwoWins_ReturnsSix()
    {
        // Arrange
        var matches = new List<MatchResult> { W(), W() };

        // Act
        var score = _calculator.CalculateScore(matches);

        // Assert
        score.Should().Be(6, "because 3+3 = 6 points");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-002")]
    public void CalculateScore_ThreeDraws_ReturnsThree()
    {
        // Arrange
        var matches = new List<MatchResult> { D(), D(), D() };

        // Act
        var score = _calculator.CalculateScore(matches);

        // Assert
        score.Should().Be(3, "because 1+1+1 = 3 points");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-003")]
    public void CalculateScore_TwoLosses_ReturnsZero()
    {
        // Arrange
        var matches = new List<MatchResult> { L(), L() };

        // Act
        var score = _calculator.CalculateScore(matches);

        // Assert
        score.Should().Be(0, "because defeats give 0 points");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-004")]
    public void CalculateScore_ThreeConsecutiveWins_ReturnsFourteen()
    {
        // Arrange
        var matches = new List<MatchResult> { W(), W(), W() };

        // Act
        var score = _calculator.CalculateScore(matches);

        // Assert
        score.Should().Be(14, "because 9 points + 5 streak bonus");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-004")]
    public void CalculateScore_FourConsecutiveWins_ReturnsSeventeen()
    {
        // Arrange
        var matches = new List<MatchResult> { W(), W(), W(), W() };

        // Act
        var score = _calculator.CalculateScore(matches);

        // Assert
        score.Should().Be(17, "because 12 points + 5 bonus (series bonus counted once regardless of length)");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-004")]
    public void CalculateScore_WinsInterrupted_NoBonus()
    {
        // Arrange
        var matches = new List<MatchResult> { W(), W(), L(), W() };

        // Act
        var score = _calculator.CalculateScore(matches);

        // Assert
        score.Should().Be(9, "because 3+3+0+3=9 with no series of 3 consecutive wins");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-005")]
    public void CalculateScore_TwoDistinctSeries_TwoBonuses()
    {
        var matches = new List<MatchResult> { W(), W(), W(), L(), W(), W(), W(), W() };

        // Act
        var score = _calculator.CalculateScore(matches);

        // Assert
        score.Should().Be(31, "because 21 base points + 5 bonus (first series of 3) + 5 bonus (second series of 4)");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-004")]
    public void CalculateScore_DrawInterruptsStreak_NoBonus()
    {
        // Arrange
        var matches = new List<MatchResult> { W(), D(), W(), W() };

        // Act
        var score = _calculator.CalculateScore(matches);

        // Assert
        score.Should().Be(10, "because 3+1+3+3=10 — Draw breaks the streak, only 2 consecutive wins at the end");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-006")]
    public void CalculateScore_Disqualified_ReturnsZero()
    {
        // Arrange
        var matches = new List<MatchResult> { W(), W(), W() };

        // Act
        var score = _calculator.CalculateScore(matches, isDisqualified: true);

        // Assert
        score.Should().Be(0, "because disqualification sets the final score to 0 regardless of results");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-006")]
    public void CalculateScore_DisqualifiedWithNoMatches_ReturnsZero()
    {
        // Arrange
        var matches = new List<MatchResult>();

        // Act
        var score = _calculator.CalculateScore(matches, isDisqualified: true);

        // Assert
        score.Should().Be(0);
    }

    [Fact]
    [Trait("Requirement", "REQ-T-007")]
    public void CalculateScore_WithPenalty_SubtractsPenaltyPoints()
    {
        var matches = new List<MatchResult> { W(), W(), W(), W() };

        // Act
        var score = _calculator.CalculateScore(matches, penaltyPoints: 3);

        // Assert
        score.Should().Be(14, "because 17 - 3 = 14");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-008")]
    public void CalculateScore_PenaltyExceedsScore_ReturnsZero()
    {
        var matches = new List<MatchResult> { W(), D() };

        // Act
        var score = _calculator.CalculateScore(matches, penaltyPoints: 8);

        // Assert
        score.Should().Be(0, "because final score cannot be negative (4 - 8 = clamped to 0)");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-008")]
    public void CalculateScore_PenaltyEqualsScore_ReturnsZero()
    {
        var matches = new List<MatchResult> { W(), W(), D() };

        // Act
        var score = _calculator.CalculateScore(matches, penaltyPoints: 7);

        // Assert
        score.Should().Be(0, "because 7 - 7 = 0");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-011")]
    public void CalculateScore_EmptyList_ReturnsZero()
    {
        // Arrange
        var matches = new List<MatchResult>();

        // Act
        var score = _calculator.CalculateScore(matches);

        // Assert
        score.Should().Be(0);
    }

    [Fact]
    [Trait("Requirement", "REQ-T-009")]
    public void CalculateScore_NullMatches_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _calculator.CalculateScore(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("matches");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-010")]
    public void CalculateScore_NegativePenalty_ThrowsArgumentException()
    {
        // Act
        Action act = () => _calculator.CalculateScore(new List<MatchResult>(), penaltyPoints: -1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("penaltyPoints");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-001")]
    [Trait("Requirement", "REQ-T-003")]
    [Trait("Requirement", "REQ-T-004")]
    public void CalculateScore_LongTournament_CalculatesCorrectly()
    {
        var matches = Enumerable.Range(0, 100)
            .Select(i => i % 2 == 0 ? W() : L())
            .ToList();

        // Act
        var score = _calculator.CalculateScore(matches);

        // Assert
        score.Should().Be(150, "because 50 wins × 3 = 150 points, no streak bonus (always interrupted by a loss)");
    }

    [Theory]
    [Trait("Requirement", "REQ-T-001")]
    [Trait("Requirement", "REQ-T-002")]
    [Trait("Requirement", "REQ-T-003")]
    [Trait("Requirement", "REQ-T-004")]
    [InlineData(3, 0, 0, 14)]
    [InlineData(2, 1, 0, 7)]
    [InlineData(0, 0, 3, 0)]
    public void CalculateScore_VariousCombinations_ReturnsExpected(int wins, int draws, int losses, int expected)
    {
        // Arrange
        var matches = new List<MatchResult>();
        for (var i = 0; i < wins; i++) matches.Add(W());
        for (var i = 0; i < draws; i++) matches.Add(D());
        for (var i = 0; i < losses; i++) matches.Add(L());

        // Act
        var score = _calculator.CalculateScore(matches);

        // Assert
        score.Should().Be(expected);
    }

    public static IEnumerable<object[]> ComplexScenarios =>
    [
        [new[] { "Win", "Win", "Win", "Draw" }, 15],
        [new[] { "Win", "Draw", "Win", "Win" }, 10],
        [new[] { "Win", "Win", "Win", "Win", "Win" }, 20],
    ];

    [Theory]
    [Trait("Requirement", "REQ-T-004")]
    [Trait("Requirement", "REQ-T-005")]
    [MemberData(nameof(ComplexScenarios))]
    public void CalculateScore_ComplexScenarios_ReturnsExpected(string[] results, int expected)
    {
        // Arrange
        var matches = results.Select(r => r switch
        {
            "Win"  => W(),
            "Draw" => D(),
            _      => L()
        }).ToList();

        // Act
        var score = _calculator.CalculateScore(matches);

        // Assert
        score.Should().Be(expected);
    }
}
