using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class ClassesControllerTests
{
    private readonly Mock<IClassService> _mockService = new();
    private readonly ClassesController _controller;

    public ClassesControllerTests()
    {
        _controller = new ClassesController(_mockService.Object);
    }

    // ── GET /api/classes ───────────────────────────────────────

    [Fact]
    public async Task GetAllClasses_ReturnsOkWithList()
    {
        var list = new List<ClassResponse> { new(1, "Knight", "desc", 5) };
        _mockService.Setup(s => s.GetAllClassesAsync()).ReturnsAsync(list);

        var result = await _controller.GetAllClasses();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(list);
    }

    // ── GET /api/classes/{id} ──────────────────────────────────

    [Fact]
    public async Task GetClass_ExistingId_ReturnsOk()
    {
        var response = new ClassResponse(1, "Knight", "desc", 5);
        _mockService.Setup(s => s.GetClassAsync(1)).ReturnsAsync(response);

        var result = await _controller.GetClass(1);

        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetClass_UnknownId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetClassAsync(99)).ThrowsAsync(new ClassNotFoundException(99));

        var result = await _controller.GetClass(99);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    // ── GET /api/classes/{id}/skills ───────────────────────────

    [Fact]
    public async Task GetClassSkills_ExistingClass_ReturnsOk()
    {
        var skills = new List<SkillResponse>
        {
            new(1, 1, "Sword Slash", "ATTACK", 25, 0, null, "desc")
        };
        _mockService.Setup(s => s.GetClassSkillsAsync(1)).ReturnsAsync(skills);

        var result = await _controller.GetClassSkills(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(skills);
    }

    [Fact]
    public async Task GetClassSkills_UnknownClass_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetClassSkillsAsync(99)).ThrowsAsync(new ClassNotFoundException(99));

        var result = await _controller.GetClassSkills(99);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }
}
