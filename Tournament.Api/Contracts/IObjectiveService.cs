using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface IObjectiveService
{
    Task<ObjectiveResponse> CreateObjectiveAsync(CreateObjectiveRequest request);
    Task<ObjectiveResponse> GetObjectiveAsync(int id);
    Task<IEnumerable<ObjectiveResponse>> GetSeasonObjectivesAsync(int seasonId);
    Task<PlayerObjectiveProgressResponse> GetPlayerProgressAsync(int objectiveId, int playerId, string? periodKey = null);
    Task<PlayerObjectiveProgressResponse> UpdatePlayerProgressAsync(int objectiveId, int playerId, UpdateObjectiveProgressRequest request);
    Task<IEnumerable<PlayerObjectiveCompletionResponse>> GetPlayerCompletionsAsync(int objectiveId, int playerId);
}
