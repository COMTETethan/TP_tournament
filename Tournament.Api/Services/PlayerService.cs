using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Tournament.Api.Services;

public class PlayerService : IPlayerService
{
    private readonly List<PlayerEntity> _players;
    private int _nextId;
    private static readonly HashSet<int> ExistingTournaments = new() { 1 };

    public PlayerService()
    {
        _players = new List<PlayerEntity>
        {
            new() { Id = 1, TournamentId = 1, Name = "Player One", IsDisqualified = false, PenaltyPoints = 0 },
            new() { Id = 2, TournamentId = 1, Name = "Player Two", IsDisqualified = true,  PenaltyPoints = 0 }
        };
        _nextId = 3;
    }

    public Task<PlayerResponse> AddPlayerAsync(int tournamentId, CreatePlayerRequest request)
    {
        if (!ExistingTournaments.Contains(tournamentId))
            throw new TournamentNotFoundException(tournamentId);

        var entity = new PlayerEntity
        {
            Id = _nextId++,
            TournamentId = tournamentId,
            Name = request.Name,
            IsDisqualified = false,
            PenaltyPoints = 0
        };
        _players.Add(entity);
        return Task.FromResult(Map(entity));
    }

    public Task<PlayerResponse> GetPlayerAsync(int id)
    {
        var found = _players.FirstOrDefault(p => p.Id == id);
        if (found is null)
            throw new PlayerNotFoundException(id);
        return Task.FromResult(Map(found));
    }

    public Task<IEnumerable<PlayerResponse>> GetTournamentPlayersAsync(int tournamentId)
    {
        if (!ExistingTournaments.Contains(tournamentId))
            throw new TournamentNotFoundException(tournamentId);

        var results = _players.Where(p => p.TournamentId == tournamentId).Select(Map).ToList();
        return Task.FromResult<IEnumerable<PlayerResponse>>(results);
    }

    public Task<PlayerResponse> DisqualifyPlayerAsync(int id)
    {
        var p = _players.FirstOrDefault(x => x.Id == id);
        if (p is null) throw new PlayerNotFoundException(id);
        p.IsDisqualified = true;
        p.PenaltyPoints = 0;
        return Task.FromResult(Map(p));
    }

    public Task<PlayerResponse> AddPenaltyAsync(int id, AddPenaltyRequest request)
    {
        if (request.PenaltyPoints < 0)
            throw new ArgumentException("PenaltyPoints must be non-negative.", nameof(request.PenaltyPoints));

        var p = _players.FirstOrDefault(x => x.Id == id);
        if (p is null) throw new PlayerNotFoundException(id);
        p.PenaltyPoints += request.PenaltyPoints;
        return Task.FromResult(Map(p));
    }

    private static PlayerResponse Map(PlayerEntity e)
        => new(e.Id, e.TournamentId, e.Name, e.IsDisqualified, e.PenaltyPoints);

    private class PlayerEntity
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDisqualified { get; set; }
        public int PenaltyPoints { get; set; }
    }
}
