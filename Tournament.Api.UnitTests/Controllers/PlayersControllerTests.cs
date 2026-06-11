using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class PlayersControllerTests
{
    private readonly Mock<IPlayerService> _mockService = new();
    private readonly PlayersController _controller;

    public PlayersControllerTests()
    {
        _controller = new PlayersController(_mockService.Object);
        SetAuthenticatedUser(userId: 1);
    }

    private void SetAuthenticatedUser(int userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) }, "Bearer");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private void SetAnonymous()
        => _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

    // ── POST /api/players ──────────────────────────────────────

    [Fact]
    public async Task CreatePlayer_Authenticated_ReturnsCreated()
    {
        var request  = new CreatePlayerRequest("Sir Galahad", 1, 2);
        var response = new PlayerResponse(5, 1, "Sir Galahad", 1, 2);
        _mockService.Setup(s => s.CreatePlayerAsync(1, request)).ReturnsAsync(response);

        var result = await _controller.CreatePlayer(request);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task CreatePlayer_NotAuthenticated_ReturnsUnauthorized()
    {
        SetAnonymous();

        var result = await _controller.CreatePlayer(new CreatePlayerRequest("X", 1));

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreatePlayer_UnknownClass_ReturnsNotFound()
    {
        var request = new CreatePlayerRequest("X", 999);
        _mockService.Setup(s => s.CreatePlayerAsync(1, request)).ThrowsAsync(new ClassNotFoundException(999));

        var result = await _controller.CreatePlayer(request);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreatePlayer_InvalidArgument_ReturnsBadRequest()
    {
        var request = new CreatePlayerRequest("", 1);
        _mockService.Setup(s => s.CreatePlayerAsync(1, request)).ThrowsAsync(new ArgumentException("Name cannot be empty."));

        var result = await _controller.CreatePlayer(request);

        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }

    // ── GET /api/players/{id} ──────────────────────────────────

    [Fact]
    public async Task GetPlayer_ExistingId_ReturnsOk()
    {
        _mockService.Setup(s => s.GetPlayerAsync(5)).ReturnsAsync(new PlayerResponse(5, 1, "Sir Galahad", 1, 2));

        var result = await _controller.GetPlayer(5);

        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetPlayer_NonExistingId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetPlayerAsync(99)).ThrowsAsync(new PlayerNotFoundException(99));

        var result = await _controller.GetPlayer(99);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    // ── GET /api/players/me ────────────────────────────────────

    [Fact]
    public async Task GetMyPlayers_Authenticated_ReturnsOkWithList()
    {
        var list = new List<PlayerResponse> { new(1, 1, "Knight", 1, 1), new(2, 1, "Mage", 2, 1) };
        _mockService.Setup(s => s.GetUserPlayersAsync(1)).ReturnsAsync(list);

        var result = await _controller.GetMyPlayers();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(list);
    }

    [Fact]
    public async Task GetMyPlayers_NotAuthenticated_ReturnsUnauthorized()
    {
        SetAnonymous();

        var result = await _controller.GetMyPlayers();

        result.Should().BeOfType<UnauthorizedResult>();
    }
}
