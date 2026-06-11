using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Tournament.Api.Services;

/// <summary>Champions owned by users. A champion carries a class and a level, not a tournament.</summary>
public class PlayerService : IPlayerService
{
    private readonly List<PlayerEntity> _players;
    private int _nextId;

    public PlayerService()
    {
        // Two seeded champions owned by user 1 (a Knight and a Mage).
        _players = new List<PlayerEntity>
        {
            new() { Id = 1, UserId = 1, Name = "Player One", ClassId = 1, Level = 1 },
            new() { Id = 2, UserId = 1, Name = "Player Two", ClassId = 2, Level = 1 }
        };
        _nextId = 3;
    }

    public Task<PlayerResponse> CreatePlayerAsync(int userId, CreatePlayerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name cannot be empty.", nameof(request.Name));

        if (request.Level < 1)
            throw new ArgumentException("Level must be at least 1.", nameof(request.Level));

        if (!ClassCatalog.ClassExists(request.ClassId))
            throw new ClassNotFoundException(request.ClassId);

        var entity = new PlayerEntity
        {
            Id = _nextId++,
            UserId = userId,
            Name = request.Name,
            ClassId = request.ClassId,
            Level = request.Level
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

    public Task<IEnumerable<PlayerResponse>> GetUserPlayersAsync(int userId)
        => Task.FromResult<IEnumerable<PlayerResponse>>(
            _players.Where(p => p.UserId == userId).Select(Map).ToList());

    private static PlayerResponse Map(PlayerEntity e)
        => new(e.Id, e.UserId, e.Name, e.ClassId, e.Level);

    private class PlayerEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public int Level { get; set; } = 1;
    }
}
