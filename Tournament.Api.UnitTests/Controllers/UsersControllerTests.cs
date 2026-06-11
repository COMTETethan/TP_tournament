using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IAuthService> _mockService = new();
    private readonly UsersController    _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_mockService.Object);
        // Simulate an authenticated user with userId=1
        SetAuthenticatedUser(userId: 1, email: "alice@test.com");
    }

    private void SetAuthenticatedUser(int userId, string email)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
        };
        var identity  = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    // ── GET /me ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_AuthenticatedUser_Returns200WithProfile()
    {
        // Arrange
        var response = new MeResponse(1, "alice@test.com", null, DateTime.UtcNow);
        _mockService.Setup(s => s.GetMeAsync(1)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetMe();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var me = ok.Value.Should().BeOfType<MeResponse>().Subject;
        me.Id.Should().Be(1);
        me.Email.Should().Be("alice@test.com");
        me.PlayerId.Should().BeNull();
        me.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetMe_UserNotFound_Returns401()
    {
        // Arrange
        _mockService.Setup(s => s.GetMeAsync(1))
                    .ThrowsAsync(new InvalidCredentialsException());

        // Act
        var result = await _controller.GetMe();

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetMe_MissingSubClaim_Returns401()
    {
        // Arrange — controller context with no 'sub' claim
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        // Act
        var result = await _controller.GetMe();

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }
}
