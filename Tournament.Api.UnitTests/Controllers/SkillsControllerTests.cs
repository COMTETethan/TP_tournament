using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

[Trait("Category", "Class")]
[Trait("Layer", "Controller")]
public class SkillsControllerTests
{
    private readonly Mock<IClassService> _mockService = new();
    private readonly SkillsController _controller;

    public SkillsControllerTests()
    {
        _controller = new SkillsController(_mockService.Object);
    }

    [Fact]
    public async Task GetSkill_ExistingId_ReturnsOk()
    {
        // Arrange
        var response = new SkillResponse(4, 1, "War Cry", "AURA", 10, 3, "ATTACK_UP", "desc");
        _mockService.Setup(s => s.GetSkillAsync(4)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetSkill(4);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetSkill_UnknownId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetSkillAsync(9999)).ThrowsAsync(new SkillNotFoundException(9999));

        // Act
        var result = await _controller.GetSkill(9999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }
}
