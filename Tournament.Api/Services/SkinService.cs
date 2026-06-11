using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Services;

public class SkinService : ISkinService
{
    public Task<SkinResponse> CreateSkinAsync(CreateSkinRequest request) => throw new NotImplementedException();
    public Task<IEnumerable<SkinResponse>> GetAllSkinsAsync() => throw new NotImplementedException();
    public Task<SkinResponse> GetSkinAsync(int id) => throw new NotImplementedException();
    public Task DeactivateSkinAsync(int id) => throw new NotImplementedException();
    public Task<PlayerLoadoutResponse> EquipPlayerSkinAsync(int playerId, EquipPlayerSkinRequest request) => throw new NotImplementedException();
    public Task<PlayerLoadoutResponse> GetPlayerLoadoutAsync(int playerId) => throw new NotImplementedException();
    public Task<TournamentBackgroundResponse> SetTournamentBackgroundAsync(int tournamentId, SetTournamentBackgroundRequest request) => throw new NotImplementedException();
    public Task<TournamentBackgroundResponse> GetTournamentBackgroundAsync(int tournamentId) => throw new NotImplementedException();
}
