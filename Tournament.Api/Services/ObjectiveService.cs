using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Services;

public class ObjectiveService : IObjectiveService
{
    public Task<ObjectiveResponse> CreateObjectiveAsync(CreateObjectiveRequest request)
        => throw new NotImplementedException();

    public Task<ObjectiveResponse> GetObjectiveAsync(int id)
        => throw new NotImplementedException();

    public Task<IEnumerable<ObjectiveResponse>> GetSeasonObjectivesAsync(int seasonId)
        => throw new NotImplementedException();

    public Task<PlayerObjectiveProgressResponse> GetPlayerProgressAsync(int objectiveId, int playerId)
        => throw new NotImplementedException();

    public Task<PlayerObjectiveProgressResponse> UpdatePlayerProgressAsync(int objectiveId, int playerId, UpdateObjectiveProgressRequest request)
        => throw new NotImplementedException();
}
