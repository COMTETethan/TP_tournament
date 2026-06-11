using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface IReplayService
{
    Task<ReplayResponse> StartReplayAsync(int duelId);
    Task<ReplayResponse> GetReplayAsync(int duelId);
    Task<ReplayEventResponse> AddEventAsync(int duelId, AddReplayEventRequest request);
    Task<IEnumerable<ReplayEventResponse>> GetEventsAsync(int duelId);
    Task<ReplayResponse> CompleteReplayAsync(int duelId);
    Task<CosmeticSnapshotResponse> GetCosmeticSnapshotAsync(int duelId);
}
