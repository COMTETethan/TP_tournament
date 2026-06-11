using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface IObjectiveService
{
    Task<ObjectiveResponse> CreateObjectiveAsync(CreateObjectiveRequest request);
    Task<ObjectiveResponse> GetObjectiveAsync(int id);
    Task<IEnumerable<ObjectiveResponse>> GetSeasonObjectivesAsync(int seasonId);
    Task<PlayerObjectiveProgressResponse> GetPlayerProgressAsync(int objectiveId, int playerId);
    Task<PlayerObjectiveProgressResponse> UpdatePlayerProgressAsync(int objectiveId, int playerId, UpdateObjectiveProgressRequest request);
}
