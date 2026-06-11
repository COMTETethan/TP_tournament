using Dapper;
using Npgsql;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services.Db;

/// <summary>PostgreSQL-backed duels (Dapper). Duration is stored as an INTERVAL.</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class DbDuelService : IDuelService
{
    private static readonly HashSet<string> ValidOutcomes = new(StringComparer.OrdinalIgnoreCase)
    { "PLAYER1_WIN", "PLAYER2_WIN", "DRAW" };

    private const string Select = @"
        SELECT id, tournament_id, player1_id, player2_id, outcome::text AS outcome,
               duel_order, played_at, EXTRACT(EPOCH FROM duration)::int AS duration_seconds
        FROM duels";

    private readonly NpgsqlDataSource _db;
    public DbDuelService(NpgsqlDataSource db) => _db = db;

    public async Task<DuelResponse> CreateDuelAsync(int tournamentId, CreateDuelRequest request)
    {
        if (request.Player1Id == request.Player2Id)
            throw new ArgumentException("Cannot create a duel with the same player on both sides.", nameof(request));

        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleAsync<DuelResponse>(
            @"INSERT INTO duels (tournament_id, player1_id, player2_id, duel_order)
              VALUES (@tournamentId, @Player1Id, @Player2Id, @DuelOrder)
              RETURNING id, tournament_id, player1_id, player2_id, outcome::text AS outcome,
                        duel_order, played_at, EXTRACT(EPOCH FROM duration)::int AS duration_seconds",
            new { tournamentId, request.Player1Id, request.Player2Id, request.DuelOrder });
    }

    public async Task<DuelResponse> GetDuelAsync(int id)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<DuelResponse>($"{Select} WHERE id = @id", new { id })
               ?? throw new DuelNotFoundException(id);
    }

    public async Task<IEnumerable<DuelResponse>> GetTournamentDuelsAsync(int tournamentId)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QueryAsync<DuelResponse>(
            $"{Select} WHERE tournament_id = @tournamentId ORDER BY duel_order", new { tournamentId });
    }

    public async Task<DuelResponse> SetDuelOutcomeAsync(int id, SetDuelOutcomeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Outcome) || !ValidOutcomes.Contains(request.Outcome))
            throw new ArgumentException("Invalid duel outcome.", nameof(request.Outcome));

        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<DuelResponse>(
                   @"UPDATE duels SET outcome = @Outcome::duel_outcome WHERE id = @id
                     RETURNING id, tournament_id, player1_id, player2_id, outcome::text AS outcome,
                               duel_order, played_at, EXTRACT(EPOCH FROM duration)::int AS duration_seconds",
                   new { id, request.Outcome })
               ?? throw new DuelNotFoundException(id);
    }

    public async Task<DuelResponse> EndDuelAsync(int id, EndDuelRequest request)
    {
        await using var conn = await _db.OpenConnectionAsync();

        var current = await conn.QuerySingleOrDefaultAsync<int?>(
            "SELECT EXTRACT(EPOCH FROM duration)::int FROM duels WHERE id = @id", new { id });
        // QuerySingleOrDefault on a non-existent row returns default(int?) = null, same as a NULL duration;
        // disambiguate with an explicit existence check.
        if (!await conn.ExecuteScalarAsync<bool>("SELECT EXISTS (SELECT 1 FROM duels WHERE id = @id)", new { id }))
            throw new DuelNotFoundException(id);
        if (current is not null)
            throw new DuelAlreadyEndedException(id);

        return await conn.QuerySingleAsync<DuelResponse>(
            @"UPDATE duels SET duration = make_interval(secs => @DurationSeconds) WHERE id = @id
              RETURNING id, tournament_id, player1_id, player2_id, outcome::text AS outcome,
                        duel_order, played_at, EXTRACT(EPOCH FROM duration)::int AS duration_seconds",
            new { id, request.DurationSeconds });
    }
}
