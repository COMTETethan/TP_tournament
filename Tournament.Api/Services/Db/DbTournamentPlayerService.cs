using Dapper;
using Npgsql;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services.Db;

/// <summary>PostgreSQL-backed tournament registrations (Dapper).</summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class DbTournamentPlayerService : ITournamentPlayerService
{
    private const string SelectJoin = @"
        SELECT tp.tournament_id, tp.player_id, p.name AS player_name,
               p.class_id, p.level, tp.is_disqualified, tp.penalty_points
        FROM tournament_players tp
        JOIN players p ON p.id = tp.player_id";

    private readonly NpgsqlDataSource _db;
    public DbTournamentPlayerService(NpgsqlDataSource db) => _db = db;

    public async Task<RegistrationResponse> RegisterAsync(int tournamentId, int playerId)
    {
        await using var conn = await _db.OpenConnectionAsync();

        if (!await conn.ExecuteScalarAsync<bool>("SELECT EXISTS (SELECT 1 FROM tournaments WHERE id = @tournamentId)", new { tournamentId }))
            throw new TournamentNotFoundException(tournamentId);
        if (!await conn.ExecuteScalarAsync<bool>("SELECT EXISTS (SELECT 1 FROM players WHERE id = @playerId)", new { playerId }))
            throw new PlayerNotFoundException(playerId);
        if (await conn.ExecuteScalarAsync<bool>("SELECT EXISTS (SELECT 1 FROM tournament_players WHERE tournament_id = @tournamentId AND player_id = @playerId)", new { tournamentId, playerId }))
            throw new PlayerAlreadyRegisteredException(tournamentId, playerId);

        await conn.ExecuteAsync(
            "INSERT INTO tournament_players (tournament_id, player_id) VALUES (@tournamentId, @playerId)",
            new { tournamentId, playerId });

        return await GetRegistrationAsync(tournamentId, playerId);
    }

    public async Task<IEnumerable<RegistrationResponse>> GetTournamentPlayersAsync(int tournamentId)
    {
        await using var conn = await _db.OpenConnectionAsync();
        if (!await conn.ExecuteScalarAsync<bool>("SELECT EXISTS (SELECT 1 FROM tournaments WHERE id = @tournamentId)", new { tournamentId }))
            throw new TournamentNotFoundException(tournamentId);

        return await conn.QueryAsync<RegistrationResponse>(
            $"{SelectJoin} WHERE tp.tournament_id = @tournamentId ORDER BY tp.player_id", new { tournamentId });
    }

    public async Task<RegistrationResponse> GetRegistrationAsync(int tournamentId, int playerId)
    {
        await using var conn = await _db.OpenConnectionAsync();
        return await conn.QuerySingleOrDefaultAsync<RegistrationResponse>(
                   $"{SelectJoin} WHERE tp.tournament_id = @tournamentId AND tp.player_id = @playerId",
                   new { tournamentId, playerId })
               ?? throw new RegistrationNotFoundException(tournamentId, playerId);
    }

    public async Task<RegistrationResponse> DisqualifyAsync(int tournamentId, int playerId)
    {
        await using var conn = await _db.OpenConnectionAsync();
        var rows = await conn.ExecuteAsync(
            "UPDATE tournament_players SET is_disqualified = TRUE, penalty_points = 0 " +
            "WHERE tournament_id = @tournamentId AND player_id = @playerId",
            new { tournamentId, playerId });
        if (rows == 0) throw new RegistrationNotFoundException(tournamentId, playerId);
        return await GetRegistrationAsync(tournamentId, playerId);
    }

    public async Task<RegistrationResponse> AddPenaltyAsync(int tournamentId, int playerId, AddPenaltyRequest request)
    {
        if (request.PenaltyPoints < 0)
            throw new ArgumentException("PenaltyPoints must be non-negative.", nameof(request.PenaltyPoints));

        await using var conn = await _db.OpenConnectionAsync();
        var rows = await conn.ExecuteAsync(
            "UPDATE tournament_players SET penalty_points = penalty_points + @PenaltyPoints " +
            "WHERE tournament_id = @tournamentId AND player_id = @playerId",
            new { tournamentId, playerId, request.PenaltyPoints });
        if (rows == 0) throw new RegistrationNotFoundException(tournamentId, playerId);
        return await GetRegistrationAsync(tournamentId, playerId);
    }
}
