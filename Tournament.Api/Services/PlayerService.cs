using Microsoft.EntityFrameworkCore;
using Tournament.Api.Contracts;
using Tournament.Api.Data;
using Tournament.Api.Data.Entities;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

public class PlayerService : IPlayerService
{
    private readonly TournamentDbContext _db;

    public PlayerService(TournamentDbContext db) => _db = db;

    public async Task<PlayerResponse> AddPlayerAsync(int tournamentId, CreatePlayerRequest request)
    {
        var exists = await _db.Tournaments.AnyAsync(t => t.Id == tournamentId);
        if (!exists)
            throw new TournamentNotFoundException(tournamentId);

        var entity = new PlayerEntity
        {
            TournamentId   = tournamentId,
            Name           = request.Name,
            IsDisqualified = false,
            PenaltyPoints  = 0,
            CreatedAt      = DateTime.UtcNow
        };
        _db.Players.Add(entity);
        await _db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<PlayerResponse> GetPlayerAsync(int id)
    {
        var entity = await _db.Players.FindAsync(id)
            ?? throw new PlayerNotFoundException(id);
        return Map(entity);
    }

    public async Task<IEnumerable<PlayerResponse>> GetTournamentPlayersAsync(int tournamentId)
    {
        var exists = await _db.Tournaments.AnyAsync(t => t.Id == tournamentId);
        if (!exists)
            throw new TournamentNotFoundException(tournamentId);

        var players = await _db.Players
            .Where(p => p.TournamentId == tournamentId)
            .ToListAsync();
        return players.Select(Map);
    }

    public async Task<PlayerResponse> DisqualifyPlayerAsync(int id)
    {
        var entity = await _db.Players.FindAsync(id)
            ?? throw new PlayerNotFoundException(id);
        entity.IsDisqualified = true;
        entity.PenaltyPoints  = 0;
        await _db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<PlayerResponse> AddPenaltyAsync(int id, AddPenaltyRequest request)
    {
        if (request.PenaltyPoints < 0)
            throw new ArgumentException("PenaltyPoints must be non-negative.", nameof(request.PenaltyPoints));

        var entity = await _db.Players.FindAsync(id)
            ?? throw new PlayerNotFoundException(id);
        entity.PenaltyPoints += request.PenaltyPoints;
        await _db.SaveChangesAsync();
        return Map(entity);
    }

    private static PlayerResponse Map(PlayerEntity e)
        => new(e.Id, e.TournamentId, e.Name, e.IsDisqualified, e.PenaltyPoints);
}
