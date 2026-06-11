using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

public class ReplayService : IReplayService
{
    private static readonly object Lock = new();
    private static readonly List<ReplayEntity> ReplayStore = new();
    private static readonly List<ReplayEventEntity> EventStore = new();
    private static readonly List<CosmeticSnapshotEntity> SnapshotStore = new();
    private static int NextReplayId = 1;
    private static long NextEventId = 1;

    // Mirrors the seed in DuelService
    private static readonly HashSet<int> KnownDuelIds = new() { 1, 2 };

    private static readonly HashSet<string> ValidEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "DUEL_START", "ATTACK", "PARRY", "TOUCH", "PENALTY", "TIMEOUT", "DISQUALIFY", "DUEL_END"
    };

    static ReplayService()
    {
        SnapshotStore.Add(new CosmeticSnapshotEntity { DuelId = 1 });
        SnapshotStore.Add(new CosmeticSnapshotEntity { DuelId = 2 });
    }

    public Task<ReplayResponse> StartReplayAsync(int duelId)
    {
        if (!KnownDuelIds.Contains(duelId))
            throw new DuelNotFoundException(duelId);

        lock (Lock)
        {
            var existing = ReplayStore.FirstOrDefault(r => r.DuelId == duelId);
            if (existing is not null)
            {
                EventStore.RemoveAll(e => e.ReplayId == existing.Id);
                existing.IsComplete = false;
                existing.RecordedAt = DateTime.UtcNow;
                return Task.FromResult(Map(existing));
            }

            var entity = new ReplayEntity
            {
                Id = NextReplayId++,
                DuelId = duelId,
                SchemaVersion = 1,
                RecordedAt = DateTime.UtcNow,
                IsComplete = false
            };
            ReplayStore.Add(entity);
            return Task.FromResult(Map(entity));
        }
    }

    public Task<ReplayResponse> GetReplayAsync(int duelId)
    {
        lock (Lock)
        {
            var replay = ReplayStore.FirstOrDefault(r => r.DuelId == duelId)
                         ?? throw new ReplayNotFoundException(duelId);
            return Task.FromResult(Map(replay));
        }
    }

    public Task<ReplayEventResponse> AddEventAsync(int duelId, AddReplayEventRequest request)
    {
        if (!ValidEventTypes.Contains(request.EventType))
            throw new ArgumentException($"Invalid event type '{request.EventType}'.", nameof(request.EventType));

        lock (Lock)
        {
            var replay = ReplayStore.FirstOrDefault(r => r.DuelId == duelId)
                         ?? throw new ReplayNotFoundException(duelId);

            var entity = new ReplayEventEntity
            {
                Id = NextEventId++,
                ReplayId = replay.Id,
                EventOrder = EventStore.Count(e => e.ReplayId == replay.Id) + 1,
                EventType = request.EventType,
                ActorPlayerId = request.ActorPlayerId,
                TargetPlayerId = request.TargetPlayerId,
                OccurredAtMs = request.OccurredAtMs,
                Payload = request.Payload
            };
            EventStore.Add(entity);
            return Task.FromResult(MapEvent(entity));
        }
    }

    public Task<IEnumerable<ReplayEventResponse>> GetEventsAsync(int duelId)
    {
        lock (Lock)
        {
            var replay = ReplayStore.FirstOrDefault(r => r.DuelId == duelId)
                         ?? throw new ReplayNotFoundException(duelId);

            var events = EventStore
                .Where(e => e.ReplayId == replay.Id)
                .OrderBy(e => e.EventOrder)
                .Select(MapEvent)
                .ToList();

            return Task.FromResult<IEnumerable<ReplayEventResponse>>(events);
        }
    }

    public Task<ReplayResponse> CompleteReplayAsync(int duelId)
    {
        lock (Lock)
        {
            var replay = ReplayStore.FirstOrDefault(r => r.DuelId == duelId)
                         ?? throw new ReplayNotFoundException(duelId);
            replay.IsComplete = true;
            return Task.FromResult(Map(replay));
        }
    }

    public Task<CosmeticSnapshotResponse> GetCosmeticSnapshotAsync(int duelId)
    {
        lock (Lock)
        {
            var snapshot = SnapshotStore.FirstOrDefault(s => s.DuelId == duelId);
            if (snapshot is null && !KnownDuelIds.Contains(duelId))
                throw new DuelNotFoundException(duelId);

            snapshot ??= new CosmeticSnapshotEntity { DuelId = duelId };
            return Task.FromResult(MapSnapshot(snapshot));
        }
    }

    private static ReplayResponse Map(ReplayEntity e)
        => new(e.Id, e.DuelId, e.SchemaVersion, e.RecordedAt, e.IsComplete);

    private static ReplayEventResponse MapEvent(ReplayEventEntity e)
        => new(e.Id, e.ReplayId, e.EventOrder, e.EventType,
               e.ActorPlayerId, e.TargetPlayerId, e.OccurredAtMs, e.Payload);

    private static CosmeticSnapshotResponse MapSnapshot(CosmeticSnapshotEntity e)
        => new(e.DuelId,
               e.Player1SkinName, e.Player1AssetKey,
               e.Player2SkinName, e.Player2AssetKey,
               e.BackgroundSkinName, e.BackgroundAssetKey);

    private class ReplayEntity
    {
        public int Id { get; set; }
        public int DuelId { get; set; }
        public int SchemaVersion { get; set; }
        public DateTime RecordedAt { get; set; }
        public bool IsComplete { get; set; }
    }

    private class ReplayEventEntity
    {
        public long Id { get; set; }
        public int ReplayId { get; set; }
        public int EventOrder { get; set; }
        public string EventType { get; set; } = "";
        public int? ActorPlayerId { get; set; }
        public int? TargetPlayerId { get; set; }
        public int OccurredAtMs { get; set; }
        public string? Payload { get; set; }
    }

    private class CosmeticSnapshotEntity
    {
        public int DuelId { get; set; }
        public string? Player1SkinName { get; set; }
        public string? Player1AssetKey { get; set; }
        public string? Player2SkinName { get; set; }
        public string? Player2AssetKey { get; set; }
        public string? BackgroundSkinName { get; set; }
        public string? BackgroundAssetKey { get; set; }
    }
}
