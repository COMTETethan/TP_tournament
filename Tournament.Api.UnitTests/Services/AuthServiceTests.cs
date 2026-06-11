using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class AuthServiceTests
{
    // Each test uses a unique email to avoid static-store collision
    private static string UniqueEmail() => $"user_{Guid.NewGuid():N}@test.com";

    private readonly AuthService _service = new("test-secret-key-min-32-chars-long!!!");

    // ── RegisterAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // Act
        var result = await _service.RegisterAsync(new RegisterRequest(UniqueEmail(), "Password1!"));

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        result.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsEmailAlreadyRegisteredException()
    {
        // Arrange
        var email = UniqueEmail();
        await _service.RegisterAsync(new RegisterRequest(email, "Password1!"));

        // Act
        Func<Task> act = () => _service.RegisterAsync(new RegisterRequest(email, "OtherPass1!"));

        // Assert
        await act.Should().ThrowAsync<EmailAlreadyRegisteredException>()
                 .Where(e => e.Email == email);
    }

    [Fact]
    public async Task RegisterAsync_EmptyEmail_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = () => _service.RegisterAsync(new RegisterRequest("", "Password1!"));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RegisterAsync_ShortPassword_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = () => _service.RegisterAsync(new RegisterRequest(UniqueEmail(), "abc"));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── LoginAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var email = UniqueEmail();
        await _service.RegisterAsync(new RegisterRequest(email, "Password1!"));

        // Act
        var result = await _service.LoginAsync(new LoginRequest(email, "Password1!"));

        // Assert
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentialsException()
    {
        // Arrange
        var email = UniqueEmail();
        await _service.RegisterAsync(new RegisterRequest(email, "CorrectPass1!"));

        // Act
        Func<Task> act = () => _service.LoginAsync(new LoginRequest(email, "WrongPass1!"));

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsInvalidCredentialsException()
    {
        // Act
        Func<Task> act = () => _service.LoginAsync(new LoginRequest("ghost@nowhere.com", "Pass123!"));

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    // ── RefreshAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_ValidRefreshToken_ReturnsNewTokenPair()
    {
        // Arrange
        var email    = UniqueEmail();
        var first    = await _service.RegisterAsync(new RegisterRequest(email, "Password1!"));

        // Act
        var result   = await _service.RefreshAsync(new RefreshTokenRequest(first.RefreshToken));

        // Assert
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBe(first.RefreshToken, "tokens rotate on each refresh");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task RefreshAsync_InvalidToken_ThrowsInvalidRefreshTokenException()
    {
        // Act
        Func<Task> act = () => _service.RefreshAsync(new RefreshTokenRequest("bogus-token"));

        // Assert
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task RefreshAsync_OldTokenAfterRotation_ThrowsInvalidRefreshTokenException()
    {
        // Arrange — refresh once to rotate the token
        var email    = UniqueEmail();
        var first    = await _service.RegisterAsync(new RegisterRequest(email, "Password1!"));
        await _service.RefreshAsync(new RefreshTokenRequest(first.RefreshToken));

        // Act — try to use the old refresh token again
        Func<Task> act = () => _service.RefreshAsync(new RefreshTokenRequest(first.RefreshToken));

        // Assert
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    // ── GetMeAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMeAsync_ExistingUser_ReturnsMeResponse()
    {
        // Arrange
        var email  = UniqueEmail();
        await _service.RegisterAsync(new RegisterRequest(email, "Password1!"));

        // Decode userId from the token to call GetMeAsync correctly
        var login  = await _service.LoginAsync(new LoginRequest(email, "Password1!"));
        var userId = GetUserIdFromToken(login.AccessToken);

        // Act
        var me = await _service.GetMeAsync(userId);

        // Assert
        me.Id.Should().Be(userId);
        me.Email.Should().Be(email.ToLowerInvariant());
        me.PlayerId.Should().BeNull();
        me.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetMeAsync_UnknownUserId_ThrowsInvalidCredentialsException()
    {
        // Act
        Func<Task> act = () => _service.GetMeAsync(99999);

        // Assert
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    // ── IConfiguration constructor ──────────────────────────────────────────

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void Constructor_WithFullConfiguration_InitializesService()
    {
        var config = BuildConfig(new()
        {
            ["Jwt:Secret"]                = "a-super-secret-key-at-least-32-chars!!",
            ["Jwt:Issuer"]                = "TestIssuer",
            ["Jwt:Audience"]              = "TestAudience",
            ["Jwt:AccessTokenExpiryHours"]  = "8",
            ["Jwt:RefreshTokenExpiryDays"]  = "14",
        });

        var svc = new AuthService(config);

        svc.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithMissingJwtSecret_ThrowsInvalidOperationException()
    {
        var config = BuildConfig(new() { ["Jwt:Secret"] = null });

        Action act = () => _ = new AuthService(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Secret*");
    }

    [Fact]
    public void Constructor_WithOnlySecret_UsesDefaultsForOptionalValues()
    {
        var config = BuildConfig(new() { ["Jwt:Secret"] = "a-super-secret-key-at-least-32-chars!!" });

        var svc = new AuthService(config);

        svc.Should().NotBeNull();
    }

    // Helper: extract 'sub' claim from JWT without validating signature
    private static int GetUserIdFromToken(string token)
    {
        var parts   = token.Split('.');
        var payload = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(PadBase64(parts[1])));
        var doc     = System.Text.Json.JsonDocument.Parse(payload);
        return int.Parse(doc.RootElement.GetProperty("sub").GetString()!);
    }

    private static string PadBase64(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        return (s.Length % 4) switch
        {
            2 => s + "==",
            3 => s + "=",
            _ => s
        };
    }
}
