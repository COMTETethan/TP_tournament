using Dapper;
using Npgsql;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services.Db;

/// <summary>PostgreSQL-backed champions owned by users (Dapper).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class DbPlayerService : IPlayerService
{
    private const string Select = "SELECT id, user_id, name, class_id, level FROM players";

    private readonly NpgsqlDataSource _db;
    public DbPlayerService(NpgsqlDataSource db) => _db = db;

    public async Task<PlayerResponse> CreatePlayerAsync(int userId, CreatePlayerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name cannot be empty.", nameof(request.Name));
        if (request.Level < 1)
            throw new ArgumentException("Level must be at least 1.", nameof(request.Level));
        if (!ClassCatalog.ClassExists(request.ClassId))
            throw new ClassNotFoundException(request.ClassId);

        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleAsync<PlayerResponse>(
            "INSERT INTO players (user_id, name, class_id, level) " +
            "VALUES (@userId, @Name, @ClassId, @Level) " +
            "RETURNING id, user_id, name, class_id, level",
            new { userId, request.Name, request.ClassId, request.Level });
    }

    public async Task<PlayerResponse> GetPlayerAsync(int id)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<PlayerResponse>(
                   $"{Select} WHERE id = @id", new { id })
               ?? throw new PlayerNotFoundException(id);
    }

    public async Task<IEnumerable<PlayerResponse>> GetUserPlayersAsync(int userId)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QueryAsync<PlayerResponse>(
            $"{Select} WHERE user_id = @userId ORDER BY id", new { userId });
    }
}
