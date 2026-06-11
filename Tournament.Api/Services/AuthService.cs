using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

public class AuthService : IAuthService
{
    private static readonly object Lock = new();
    private static readonly List<UserEntity> UserStore = new();
    private static int NextUserId = 1;

    // ── Shared read helpers for the social features (friends / challenges) ───────
    /// <summary>All registered users as (id, email) pairs.</summary>
    public static IReadOnlyList<(int Id, string Email)> AllUsers()
    {
        lock (Lock) return UserStore.Select(u => (u.Id, u.Email)).ToList();
    }

    /// <summary>True if a user with this id exists.</summary>
    public static bool UserExists(int id)
    {
        lock (Lock) return UserStore.Any(u => u.Id == id);
    }

    /// <summary>The email of the user with this id, or null.</summary>
    public static string? EmailOf(int id)
    {
        lock (Lock) return UserStore.FirstOrDefault(u => u.Id == id)?.Email;
    }

    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpiryHours;
    private readonly int _refreshTokenExpiryDays;

    public AuthService(IConfiguration configuration)
    {
        _secret               = configuration["Jwt:Secret"]                                  ?? throw new InvalidOperationException("Jwt:Secret not configured.");
        _issuer               = configuration["Jwt:Issuer"]                                  ?? "TournamentApi";
        _audience             = configuration["Jwt:Audience"]                                ?? "TournamentApiClients";
        _accessTokenExpiryHours  = int.TryParse(configuration["Jwt:AccessTokenExpiryHours"],  out var h) ? h : 4;
        _refreshTokenExpiryDays  = int.TryParse(configuration["Jwt:RefreshTokenExpiryDays"],  out var d) ? d : 7;
    }

    // Constructor for unit tests — injects settings directly without IConfiguration
    public AuthService(string secret, string issuer = "TournamentApi", string audience = "TournamentApiClients",
                       int accessTokenExpiryHours = 4, int refreshTokenExpiryDays = 7)
    {
        _secret                 = secret;
        _issuer                 = issuer;
        _audience               = audience;
        _accessTokenExpiryHours = accessTokenExpiryHours;
        _refreshTokenExpiryDays = refreshTokenExpiryDays;
    }

    public Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.", nameof(request.Email));
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters.", nameof(request.Password));

        lock (Lock)
        {
            if (UserStore.Any(u => string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase)))
                throw new EmailAlreadyRegisteredException(request.Email);

            var entity = new UserEntity
            {
                Id           = NextUserId++,
                Email        = request.Email.ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt    = DateTime.UtcNow
            };
            UserStore.Add(entity);

            return Task.FromResult(BuildTokenPair(entity));
        }
    }

    public Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        UserEntity? user;
        lock (Lock)
        {
            user = UserStore.FirstOrDefault(u =>
                string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase));
        }

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        lock (Lock)
        {
            return Task.FromResult(BuildTokenPair(user));
        }
    }

    public Task<AuthResponse> RefreshAsync(RefreshTokenRequest request)
    {
        UserEntity? user;
        lock (Lock)
        {
            user = UserStore.FirstOrDefault(u =>
                u.RefreshToken == request.RefreshToken &&
                u.RefreshTokenExpiresAt > DateTime.UtcNow);
        }

        if (user is null)
            throw new InvalidRefreshTokenException();

        lock (Lock)
        {
            return Task.FromResult(BuildTokenPair(user));
        }
    }

    public Task<MeResponse> GetMeAsync(int userId)
    {
        UserEntity? user;
        lock (Lock)
        {
            user = UserStore.FirstOrDefault(u => u.Id == userId);
        }

        if (user is null)
            throw new InvalidCredentialsException();

        return Task.FromResult(new MeResponse(user.Id, user.Email, user.PlayerId, user.CreatedAt));
    }

    // Must be called under Lock — mutates the entity's refresh token
    private AuthResponse BuildTokenPair(UserEntity user)
    {
        var expiresAt    = DateTime.UtcNow.AddHours(_accessTokenExpiryHours);
        var accessToken  = GenerateAccessToken(user, expiresAt);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken           = refreshToken;
        user.RefreshTokenExpiresAt  = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

        return new AuthResponse(accessToken, refreshToken, expiresAt);
    }

    private string GenerateAccessToken(UserEntity user, DateTime expiresAt)
    {
        var key         = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private class UserEntity
    {
        public int       Id                    { get; set; }
        public string    Email                 { get; set; } = "";
        public string    PasswordHash          { get; set; } = "";
        public int?      PlayerId              { get; set; }
        public string?   RefreshToken          { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public DateTime  CreatedAt             { get; set; }
    }
}
