using Microsoft.EntityFrameworkCore;
using Tournament.Api.Contracts;
using Tournament.Api.Data;
using Tournament.Api.Data.Entities;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

public class DuelService : IDuelService
{
    private readonly TournamentDbContext _db;
    private static readonly HashSet<string> ValidOutcomes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PLAYER1_WIN", "PLAYER2_WIN", "DRAW"
    };

    public DuelService(TournamentDbContext db) => _db = db;

    public async Task<DuelResponse> CreateDuelAsync(int tournamentId, CreateDuelRequest request)
    {
        if (request.Player1Id == request.Player2Id)
            throw new ArgumentException("Cannot create a duel with the same player as both participants (same player).", nameof(request));

        var exists = await _db.Tournaments.AnyAsync(t => t.Id == tournamentId);
        if (!exists)
            throw new TournamentNotFoundException(tournamentId);

        var entity = new DuelEntity
        {
            TournamentId = tournamentId,
            Player1Id    = request.Player1Id,
            Player2Id    = request.Player2Id,
            DuelOrder    = request.DuelOrder,
            Outcome      = null,
            PlayedAt     = DateTime.UtcNow,
            Duration     = null
        };
        _db.Duels.Add(entity);
        await _db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<DuelResponse> GetDuelAsync(int id)
    {
        var entity = await _db.Duels.FindAsync(id)
            ?? throw new DuelNotFoundException(id);
        return Map(entity);
    }

    public async Task<IEnumerable<DuelResponse>> GetTournamentDuelsAsync(int tournamentId)
    {
        var duels = await _db.Duels
            .Where(d => d.TournamentId == tournamentId)
            .ToListAsync();
        return duels.Select(Map);
    }

    public async Task<DuelResponse> SetDuelOutcomeAsync(int id, SetDuelOutcomeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Outcome) || !ValidOutcomes.Contains(request.Outcome))
            throw new ArgumentException("Invalid duel outcome.", nameof(request.Outcome));

        var entity = await _db.Duels.FindAsync(id)
            ?? throw new DuelNotFoundException(id);
        entity.Outcome = Enum.Parse<DuelOutcome>(request.Outcome, ignoreCase: true);
        await _db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<DuelResponse> EndDuelAsync(int id, EndDuelRequest request)
    {
        var entity = await _db.Duels.FindAsync(id)
            ?? throw new DuelNotFoundException(id);
        if (entity.Duration.HasValue)
            throw new DuelAlreadyEndedException(id);
        entity.Duration = TimeSpan.FromSeconds(request.DurationSeconds);
        await _db.SaveChangesAsync();
        return Map(entity);
    }

    private static DuelResponse Map(DuelEntity e)
        => new(e.Id, e.TournamentId, e.Player1Id, e.Player2Id,
               e.Outcome?.ToString(), e.DuelOrder, e.PlayedAt,
               e.Duration.HasValue ? (int?)e.Duration.Value.TotalSeconds : null);
}
