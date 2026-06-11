using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

[Trait("Category", "Class")]
[Trait("Layer", "Controller")]
public class ClassesControllerTests
{
    private readonly Mock<IClassService> _mockService = new();
    private readonly ClassesController _controller;

    public ClassesControllerTests()
    {
        _controller = new ClassesController(_mockService.Object);
    }

    [Fact]
    public async Task GetAllClasses_ReturnsOkWithList()
    {
        // Arrange
        var list = new List<ClassResponse> { new(1, "Knight", "desc", 5) };
        _mockService.Setup(s => s.GetAllClassesAsync()).ReturnsAsync(list);

        // Act
        var result = await _controller.GetAllClasses();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(list);
    }

    [Fact]
    public async Task GetClass_ExistingId_ReturnsOk()
    {
        // Arrange
        var response = new ClassResponse(1, "Knight", "desc", 5);
        _mockService.Setup(s => s.GetClassAsync(1)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetClass(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetClass_UnknownId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetClassAsync(99)).ThrowsAsync(new ClassNotFoundException(99));

        // Act
        var result = await _controller.GetClass(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetClassSkills_ExistingClass_ReturnsOk()
    {
        // Arrange
        var skills = new List<SkillResponse>
        {
            new(1, 1, "Sword Slash", "ATTACK", 25, 0, null, "desc")
        };
        _mockService.Setup(s => s.GetClassSkillsAsync(1)).ReturnsAsync(skills);

        // Act
        var result = await _controller.GetClassSkills(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(skills);
    }

    [Fact]
    public async Task GetClassSkills_UnknownClass_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetClassSkillsAsync(99)).ThrowsAsync(new ClassNotFoundException(99));

        // Act
        var result = await _controller.GetClassSkills(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }
}
