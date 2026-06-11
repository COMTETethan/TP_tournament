using Microsoft.EntityFrameworkCore;
using Tournament.Api.Contracts;
using Tournament.Api.Data;
using Tournament.Api.Data.Entities;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

public class TournamentService : ITournamentService
{
    private readonly TournamentDbContext _db;
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "OPEN", "IN_PROGRESS", "CLOSED"
    };

    public TournamentService(TournamentDbContext db) => _db = db;

    public async Task<TournamentResponse> CreateTournamentAsync(CreateTournamentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name cannot be empty.", nameof(request.Name));

        var entity = new TournamentEntity
        {
            Name      = request.Name,
            Status    = TournamentStatus.OPEN,
            CreatedAt = DateTime.UtcNow
        };
        _db.Tournaments.Add(entity);
        await _db.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<TournamentResponse> GetTournamentAsync(int id)
    {
        var entity = await _db.Tournaments.FindAsync(id)
            ?? throw new TournamentNotFoundException(id);
        return Map(entity);
    }

    public async Task<IEnumerable<TournamentResponse>> GetAllTournamentsAsync()
    {
        var list = await _db.Tournaments.ToListAsync();
        return list.Select(Map);
    }

    public async Task<TournamentResponse> UpdateTournamentStatusAsync(int id, UpdateTournamentStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status) || !ValidStatuses.Contains(request.Status))
            throw new InvalidTournamentStatusException(request.Status);

        var entity = await _db.Tournaments.FindAsync(id)
            ?? throw new TournamentNotFoundException(id);

        entity.Status = Enum.Parse<TournamentStatus>(request.Status, ignoreCase: true);
        await _db.SaveChangesAsync();
        return Map(entity);
    }

    private static TournamentResponse Map(TournamentEntity e)
        => new(e.Id, e.Name, e.Status.ToString(), e.CreatedAt);
}
