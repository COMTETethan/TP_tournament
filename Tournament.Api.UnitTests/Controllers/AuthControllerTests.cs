using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockService = new();
    private readonly AuthController     _controller;

    private static readonly AuthResponse FakeAuth = new(
        AccessToken:  "access.token.here",
        RefreshToken: "refresh-token-here",
        ExpiresAt:    DateTime.UtcNow.AddHours(4));

    public AuthControllerTests() => _controller = new AuthController(_mockService.Object);

    // ── POST /auth/register ────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidRequest_Returns201()
    {
        // Arrange
        var request = new RegisterRequest("alice@test.com", "Password1!");
        _mockService.Setup(s => s.RegisterAsync(request)).ReturnsAsync(FakeAuth);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409Conflict()
    {
        // Arrange
        var request = new RegisterRequest("alice@test.com", "Password1!");
        _mockService.Setup(s => s.RegisterAsync(request))
                    .ThrowsAsync(new EmailAlreadyRegisteredException("alice@test.com"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Register_InvalidPassword_Returns400()
    {
        // Arrange
        var request = new RegisterRequest("alice@test.com", "abc");
        _mockService.Setup(s => s.RegisterAsync(request))
                    .ThrowsAsync(new ArgumentException("Password too short."));

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── POST /auth/login ───────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        // Arrange
        var request = new LoginRequest("alice@test.com", "Password1!");
        _mockService.Setup(s => s.LoginAsync(request)).ReturnsAsync(FakeAuth);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(FakeAuth);
    }

    [Fact]
    public async Task Login_WrongCredentials_Returns401()
    {
        // Arrange
        var request = new LoginRequest("alice@test.com", "wrong");
        _mockService.Setup(s => s.LoginAsync(request))
                    .ThrowsAsync(new InvalidCredentialsException());

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ── POST /auth/refresh ─────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewTokens()
    {
        // Arrange
        var request  = new RefreshTokenRequest("valid-refresh-token");
        var newAuth  = new AuthResponse("new.access.token", "new-refresh-token", DateTime.UtcNow.AddHours(4));
        _mockService.Setup(s => s.RefreshAsync(request)).ReturnsAsync(newAuth);

        // Act
        var result = await _controller.Refresh(request);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(newAuth);
    }

    [Fact]
    public async Task Refresh_ExpiredToken_Returns401()
    {
        // Arrange
        var request = new RefreshTokenRequest("expired-token");
        _mockService.Setup(s => s.RefreshAsync(request))
                    .ThrowsAsync(new InvalidRefreshTokenException());

        // Act
        var result = await _controller.Refresh(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}
