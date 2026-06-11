using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

public class ObjectiveService : IObjectiveService
{
    private readonly List<ObjectiveEntity> _objectives = new();
    private readonly List<ProgressEntity> _progress = new();
    private int _nextObjId = 1;

    public Task<ObjectiveResponse> CreateObjectiveAsync(CreateObjectiveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name cannot be empty.", nameof(request.Name));
        if (request.XpReward <= 0)
            throw new ArgumentException("XpReward must be > 0.", nameof(request.XpReward));

        var entity = new ObjectiveEntity
        {
            Id = _nextObjId++,
            SeasonId = request.SeasonId,
            Name = request.Name,
            Description = request.Description,
            ObjectiveType = request.ObjectiveType,
            TargetValue = request.TargetValue,
            XpReward = request.XpReward,
            ResetType = request.ResetType
        };
        _objectives.Add(entity);
        return Task.FromResult(Map(entity));
    }

    public Task<ObjectiveResponse> GetObjectiveAsync(int id)
        => Task.FromResult(Map(FindOrThrow(id)));

    public Task<IEnumerable<ObjectiveResponse>> GetSeasonObjectivesAsync(int seasonId)
        => Task.FromResult<IEnumerable<ObjectiveResponse>>(_objectives.Where(o => o.SeasonId == seasonId).Select(Map).ToList());

    public Task<PlayerObjectiveProgressResponse> GetPlayerProgressAsync(int objectiveId, int playerId, string? periodKey = null)
    {
        FindOrThrow(objectiveId);
        return Task.FromResult(MapProgress(GetOrCreateProgress(objectiveId, playerId)));
    }

    public Task<IEnumerable<PlayerObjectiveCompletionResponse>> GetPlayerCompletionsAsync(int objectiveId, int playerId)
        => throw new NotImplementedException();

    public Task<PlayerObjectiveProgressResponse> UpdatePlayerProgressAsync(int objectiveId, int playerId, UpdateObjectiveProgressRequest request)
    {
        var obj = FindOrThrow(objectiveId);

        var prog = GetOrCreateProgress(objectiveId, playerId);
        prog.CurrentValue = Math.Min(request.NewValue, obj.TargetValue);

        if (prog.CurrentValue >= obj.TargetValue && !prog.IsCompleted)
        {
            prog.IsCompleted = true;
            prog.CompletedAt = DateTime.UtcNow;
        }

        return Task.FromResult(MapProgress(prog));
    }

    private ObjectiveEntity FindOrThrow(int id)
        => _objectives.FirstOrDefault(o => o.Id == id) ?? throw new ObjectiveNotFoundException(id);

    private ProgressEntity GetOrCreateProgress(int objectiveId, int playerId)
    {
        var existing = _progress.FirstOrDefault(p => p.ObjectiveId == objectiveId && p.PlayerId == playerId);
        if (existing is not null) return existing;

        var prog = new ProgressEntity { ObjectiveId = objectiveId, PlayerId = playerId };
        _progress.Add(prog);
        return prog;
    }

    private static ObjectiveResponse Map(ObjectiveEntity e)
        => new(e.Id, e.SeasonId, e.Name, e.Description, e.ObjectiveType, e.TargetValue, e.XpReward, e.ResetType);

    private static PlayerObjectiveProgressResponse MapProgress(ProgressEntity e)
        => new(e.ObjectiveId, e.PlayerId, e.CurrentValue, e.IsCompleted, e.CompletedAt);

    private class ObjectiveEntity
    {
        public int Id { get; set; }
        public int SeasonId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ObjectiveType { get; set; } = string.Empty;
        public int TargetValue { get; set; }
        public int XpReward { get; set; }
        public string ResetType { get; set; } = string.Empty;
    }

    private class ProgressEntity
    {
        public int ObjectiveId { get; set; }
        public int PlayerId { get; set; }
        public int CurrentValue { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
