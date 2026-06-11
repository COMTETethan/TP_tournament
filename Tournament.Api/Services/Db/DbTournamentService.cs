using Dapper;
using Npgsql;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services.Db;

/// <summary>PostgreSQL-backed tournaments (Dapper).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class DbTournamentService : ITournamentService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    { "OPEN", "IN_PROGRESS", "CLOSED" };

    private const string Select =
        "SELECT id, name, status::text AS status, created_at FROM tournaments";

    private readonly NpgsqlDataSource _db;
    public DbTournamentService(NpgsqlDataSource db) => _db = db;

    public async Task<TournamentResponse> CreateTournamentAsync(CreateTournamentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name cannot be empty.", nameof(request.Name));

        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleAsync<TournamentResponse>(
            "INSERT INTO tournaments (name, status) VALUES (@Name, 'OPEN') " +
            "RETURNING id, name, status::text AS status, created_at",
            new { request.Name });
    }

    public async Task<TournamentResponse> GetTournamentAsync(int id)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<TournamentResponse>(
                   $"{Select} WHERE id = @id", new { id })
               ?? throw new TournamentNotFoundException(id);
    }

    public async Task<IEnumerable<TournamentResponse>> GetAllTournamentsAsync()
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QueryAsync<TournamentResponse>($"{Select} ORDER BY id");
    }

    public async Task<TournamentResponse> UpdateTournamentStatusAsync(int id, UpdateTournamentStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status) || !ValidStatuses.Contains(request.Status))
            throw new InvalidTournamentStatusException(request.Status);

        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<TournamentResponse>(
                   "UPDATE tournaments SET status = @Status::tournament_status WHERE id = @id " +
                   "RETURNING id, name, status::text AS status, created_at",
                   new { id, request.Status })
               ?? throw new TournamentNotFoundException(id);
    }
}
