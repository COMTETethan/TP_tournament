using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace Tournament.Api.Services;

public class TournamentService : ITournamentService
{
    // Shared static store across all instances for unit tests
    private static readonly List<TournamentEntity> SharedStore = new();
    private static int NextId = 1;
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "OPEN",
        "IN_PROGRESS",
        "CLOSED"
    };

    // Seed tournament 1 for tests
    static TournamentService()
    {
        lock (SharedStore)
        {
            if (SharedStore.Count == 0)
            {
                SharedStore.Add(new TournamentEntity
                {
                    Id = 1,
                    Name = "Seeded Tournament",
                    Status = "OPEN",
                    CreatedAt = DateTime.UtcNow
                });
                NextId = 2;
            }
        }
    }

    /// <summary>True if a tournament with this id exists in the shared store.</summary>
    public static bool Exists(int id)
    {
        lock (SharedStore)
            return SharedStore.Any(t => t.Id == id);
    }

    public Task<TournamentResponse> CreateTournamentAsync(CreateTournamentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name cannot be empty.", nameof(request.Name));

        int newId;
        lock (SharedStore)
        {
            newId = NextId++;
        }
        var entity = new TournamentEntity
        {
            Id = newId,
            Name = request.Name,
            Status = "OPEN",
            CreatedAt = DateTime.UtcNow
        };

        lock (SharedStore)
        {
            SharedStore.Add(entity);
        }

        return Task.FromResult(Map(entity));
    }

    public Task<TournamentResponse> GetTournamentAsync(int id)
    {
        TournamentEntity? found;
        lock (SharedStore)
        {
            found = SharedStore.FirstOrDefault(t => t.Id == id);
        }

        if (found is null)
            throw new TournamentNotFoundException(id);

        return Task.FromResult(Map(found));
    }

    public Task<IEnumerable<TournamentResponse>> GetAllTournamentsAsync()
    {
        List<TournamentEntity> snapshot;
        lock (SharedStore)
        {
            snapshot = SharedStore.ToList();
        }

        var results = snapshot.Select(Map).ToList();
        return Task.FromResult<IEnumerable<TournamentResponse>>(results);
    }

    public Task<TournamentResponse> UpdateTournamentStatusAsync(int id, UpdateTournamentStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status) || !ValidStatuses.Contains(request.Status))
            throw new InvalidTournamentStatusException(request.Status);

        lock (SharedStore)
        {
            var entity = SharedStore.FirstOrDefault(t => t.Id == id);
            if (entity is null)
                throw new TournamentNotFoundException(id);

            entity.Status = request.Status;
            return Task.FromResult(Map(entity));
        }
    }

    // Local mapping and storage entity
    private static TournamentResponse Map(TournamentEntity e)
        => new(e.Id, e.Name, e.Status, e.CreatedAt);

    private class TournamentEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
