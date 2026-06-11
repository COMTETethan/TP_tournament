using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services.Db;

/// <summary>PostgreSQL-backed authentication (Dapper). Mirrors AuthService logic with DB storage.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class DbAuthService : IAuthService
{
    private readonly NpgsqlDataSource _db;
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpiryHours;
    private readonly int _refreshTokenExpiryDays;

    public DbAuthService(NpgsqlDataSource db, IConfiguration configuration)
    {
        _db = db;
        _secret               = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured.");
        _issuer               = configuration["Jwt:Issuer"] ?? "TournamentApi";
        _audience             = configuration["Jwt:Audience"] ?? "TournamentApiClients";
        _accessTokenExpiryHours = int.TryParse(configuration["Jwt:AccessTokenExpiryHours"], out var h) ? h : 4;
        _refreshTokenExpiryDays = int.TryParse(configuration["Jwt:RefreshTokenExpiryDays"], out var d) ? d : 7;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.", nameof(request.Email));
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters.", nameof(request.Password));

        await using var conn = await _db.OpenConnectionAsync();

        var existing = await conn.QuerySingleOrDefaultAsync<int?>(
            "SELECT id FROM users WHERE email = @Email",
            new { Email = request.Email.ToLowerInvariant() });

        if (existing.HasValue)
            throw new EmailAlreadyRegisteredException(request.Email);

        var hash         = await Task.Run(() => BCrypt.Net.BCrypt.HashPassword(request.Password));
        var refreshToken = GenerateRefreshToken();
        var refreshExp   = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

        var id = await conn.QuerySingleAsync<int>(
            "INSERT INTO users (email, password_hash, refresh_token, refresh_token_expires_at) " +
            "VALUES (@Email, @Hash, @Refresh, @Exp) RETURNING id",
            new { Email = request.Email.ToLowerInvariant(), Hash = hash, Refresh = refreshToken, Exp = refreshExp });

        var accessToken = BuildAccessToken(id, request.Email.ToLowerInvariant(), out var expiresAt);
        return new AuthResponse(accessToken, refreshToken, expiresAt);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        await using var conn = await _db.OpenConnectionAsync();

        var user = await conn.QuerySingleOrDefaultAsync<UserRow>(
            "SELECT id, email, password_hash, refresh_token, refresh_token_expires_at FROM users WHERE email = @Email",
            new { Email = request.Email.ToLowerInvariant() });

        var valid = user is not null && await Task.Run(() => BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash));
        if (!valid || user is null)
            throw new InvalidCredentialsException();

        var refreshToken = GenerateRefreshToken();
        var refreshExp   = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

        await conn.ExecuteAsync(
            "UPDATE users SET refresh_token = @Refresh, refresh_token_expires_at = @Exp WHERE id = @Id",
            new { user.Id, Refresh = refreshToken, Exp = refreshExp });

        var accessToken = BuildAccessToken(user.Id, user.Email, out var expiresAt);
        return new AuthResponse(accessToken, refreshToken, expiresAt);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request)
    {
        await using var conn = await _db.OpenConnectionAsync();

        var user = await conn.QuerySingleOrDefaultAsync<UserRow>(
            "SELECT id, email, refresh_token, refresh_token_expires_at FROM users " +
            "WHERE refresh_token = @Token AND refresh_token_expires_at > NOW()",
            new { Token = request.RefreshToken });

        if (user is null)
            throw new InvalidRefreshTokenException();

        var refreshToken = GenerateRefreshToken();
        var refreshExp   = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);

        await conn.ExecuteAsync(
            "UPDATE users SET refresh_token = @Refresh, refresh_token_expires_at = @Exp WHERE id = @Id",
            new { user.Id, Refresh = refreshToken, Exp = refreshExp });

        var accessToken = BuildAccessToken(user.Id, user.Email, out var expiresAt);
        return new AuthResponse(accessToken, refreshToken, expiresAt);
    }

    public async Task<MeResponse> GetMeAsync(int userId)
    {
        await using var conn = await _db.OpenConnectionAsync();

        var user = await conn.QuerySingleOrDefaultAsync<UserRow>(
            "SELECT id, email, player_id, created_at FROM users WHERE id = @userId",
            new { userId });

        if (user is null)
            throw new InvalidCredentialsException();

        return new MeResponse(user.Id, user.Email, user.PlayerId, user.CreatedAt ?? DateTime.UtcNow);
    }

    private string BuildAccessToken(int userId, string email, out DateTime expiresAt)
    {
        expiresAt = DateTime.UtcNow.AddHours(_accessTokenExpiryHours);
        var key   = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub,   userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            ],
            notBefore:          DateTime.UtcNow,
            expires:            expiresAt,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private sealed class UserRow
    {
        public int       Id            { get; init; }
        public string    Email         { get; init; } = "";
        public string    PasswordHash  { get; init; } = "";
        public int?      PlayerId      { get; init; }
        public string?   RefreshToken  { get; init; }
        public DateTime? RefreshTokenExpiresAt { get; init; }
        public DateTime? CreatedAt     { get; init; }
    }
}
