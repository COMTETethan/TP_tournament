using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Tournament.Api.Services;

public class DuelService : IDuelService
{
    private readonly List<DuelEntity> _duels;
    private int _nextId;
    private static readonly HashSet<string> ValidOutcomes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PLAYER1_WIN",
        "PLAYER2_WIN",
        "DRAW"
    };

    public DuelService()
    {
        _duels = new List<DuelEntity>
        {
            new() { Id = 1, TournamentId = 1, Player1Id = 1, Player2Id = 2, DuelOrder = 1, Outcome = "PLAYER1_WIN", DurationSeconds = 120, EndedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, TournamentId = 1, Player1Id = 1, Player2Id = 2, DuelOrder = 2, Outcome = "PLAYER1_WIN", DurationSeconds = 150, EndedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        _nextId = 3;
    }

    public Task<DuelResponse> CreateDuelAsync(int tournamentId, CreateDuelRequest request)
    {
        if (request.Player1Id == request.Player2Id)
            throw new ArgumentException("Cannot create a duel with the same player as both participants (same player).", nameof(request));

        var entity = new DuelEntity
        {
            Id = _nextId++,
            TournamentId = tournamentId,
            Player1Id = request.Player1Id,
            Player2Id = request.Player2Id,
            DuelOrder = request.DuelOrder,
            Outcome = null,
            DurationSeconds = null,
            EndedAt = null,
            CreatedAt = DateTime.UtcNow
        };
        _duels.Add(entity);
        return Task.FromResult(Map(entity));
    }

    public Task<DuelResponse> GetDuelAsync(int id)
    {
        var found = _duels.FirstOrDefault(d => d.Id == id);
        if (found is null) throw new DuelNotFoundException(id);
        return Task.FromResult(Map(found));
    }

    public Task<IEnumerable<DuelResponse>> GetTournamentDuelsAsync(int tournamentId)
    {
        var snapshot = _duels.Where(d => d.TournamentId == tournamentId).Select(Map).ToList();
        return Task.FromResult<IEnumerable<DuelResponse>>(snapshot);
    }

    public Task<DuelResponse> SetDuelOutcomeAsync(int id, SetDuelOutcomeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Outcome) || !ValidOutcomes.Contains(request.Outcome))
            throw new ArgumentException("Invalid duel outcome.", nameof(request.Outcome));

        var d = _duels.FirstOrDefault(x => x.Id == id);
        if (d is null) throw new DuelNotFoundException(id);
        d.Outcome = request.Outcome;
        return Task.FromResult(Map(d));
    }

    public Task<DuelResponse> EndDuelAsync(int id, EndDuelRequest request)
    {
        var d = _duels.FirstOrDefault(x => x.Id == id);
        if (d is null) throw new DuelNotFoundException(id);
        if (d.EndedAt is not null) throw new DuelAlreadyEndedException(id);
        d.DurationSeconds = request.DurationSeconds;
        d.EndedAt = DateTime.UtcNow;
        return Task.FromResult(Map(d));
    }

    private static DuelResponse Map(DuelEntity e)
        => new(e.Id, e.TournamentId, e.Player1Id, e.Player2Id, e.Outcome, e.DuelOrder, e.CreatedAt, e.DurationSeconds);

    private class DuelEntity
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public int Player1Id { get; set; }
        public int Player2Id { get; set; }
        public int DuelOrder { get; set; }
        public string? Outcome { get; set; }
        public int? DurationSeconds { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
