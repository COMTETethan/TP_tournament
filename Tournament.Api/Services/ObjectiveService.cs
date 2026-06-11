using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

public class ObjectiveService : IObjectiveService
{
    private readonly List<ObjectiveEntity> _objectives = new();
    private readonly List<ProgressEntity> _progress = new();
    private readonly List<CompletionEntity> _completions = new();
    private int _nextObjId = 1;
    private int _nextCompletionId = 1;

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
        return Task.FromResult(MapProgress(GetOrCreateProgress(objectiveId, playerId, periodKey)));
    }

    public Task<PlayerObjectiveProgressResponse> UpdatePlayerProgressAsync(int objectiveId, int playerId, UpdateObjectiveProgressRequest request)
    {
        var obj = FindOrThrow(objectiveId);
        var periodKey = request.PeriodKey;

        var prog = GetOrCreateProgress(objectiveId, playerId, periodKey);

        // Already completed this period — progress is frozen
        if (prog.IsCompleted)
            return Task.FromResult(MapProgress(prog));

        prog.CurrentValue = Math.Min(request.NewValue, obj.TargetValue);

        if (prog.CurrentValue >= obj.TargetValue)
        {
            prog.IsCompleted = true;
            prog.CompletedAt = DateTime.UtcNow;

            _completions.Add(new CompletionEntity
            {
                Id = _nextCompletionId++,
                PlayerId = playerId,
                ObjectiveId = objectiveId,
                CompletedAt = prog.CompletedAt.Value,
                XpAwarded = obj.XpReward,
                PeriodKey = periodKey
            });
        }

        return Task.FromResult(MapProgress(prog));
    }

    public Task<IEnumerable<PlayerObjectiveCompletionResponse>> GetPlayerCompletionsAsync(int objectiveId, int playerId)
    {
        FindOrThrow(objectiveId);
        return Task.FromResult<IEnumerable<PlayerObjectiveCompletionResponse>>(
            _completions
                .Where(c => c.ObjectiveId == objectiveId && c.PlayerId == playerId)
                .Select(MapCompletion)
                .ToList());
    }

    private ObjectiveEntity FindOrThrow(int id)
        => _objectives.FirstOrDefault(o => o.Id == id) ?? throw new ObjectiveNotFoundException(id);

    private ProgressEntity GetOrCreateProgress(int objectiveId, int playerId, string? periodKey)
    {
        var existing = _progress.FirstOrDefault(p =>
            p.ObjectiveId == objectiveId &&
            p.PlayerId == playerId &&
            p.PeriodKey == periodKey);

        if (existing is not null) return existing;

        var prog = new ProgressEntity { ObjectiveId = objectiveId, PlayerId = playerId, PeriodKey = periodKey };
        _progress.Add(prog);
        return prog;
    }

    private static ObjectiveResponse Map(ObjectiveEntity e)
        => new(e.Id, e.SeasonId, e.Name, e.Description, e.ObjectiveType, e.TargetValue, e.XpReward, e.ResetType);

    private static PlayerObjectiveProgressResponse MapProgress(ProgressEntity e)
        => new(e.ObjectiveId, e.PlayerId, e.CurrentValue, e.IsCompleted, e.CompletedAt, e.PeriodKey);

    private static PlayerObjectiveCompletionResponse MapCompletion(CompletionEntity e)
        => new(e.Id, e.PlayerId, e.ObjectiveId, e.CompletedAt, e.XpAwarded, e.PeriodKey);

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
        public string? PeriodKey { get; set; }
        public int CurrentValue { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    private class CompletionEntity
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int ObjectiveId { get; set; }
        public DateTime CompletedAt { get; set; }
        public int XpAwarded { get; set; }
        public string? PeriodKey { get; set; }
    }
}
