using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Services;

public class ReplayService : IReplayService
{
    public Task<ReplayResponse> StartReplayAsync(int duelId) => throw new NotImplementedException();
    public Task<ReplayResponse> GetReplayAsync(int duelId) => throw new NotImplementedException();
    public Task<ReplayEventResponse> AddEventAsync(int duelId, AddReplayEventRequest request) => throw new NotImplementedException();
    public Task<IEnumerable<ReplayEventResponse>> GetEventsAsync(int duelId) => throw new NotImplementedException();
    public Task<ReplayResponse> CompleteReplayAsync(int duelId) => throw new NotImplementedException();
    public Task<CosmeticSnapshotResponse> GetCosmeticSnapshotAsync(int duelId) => throw new NotImplementedException();
}
