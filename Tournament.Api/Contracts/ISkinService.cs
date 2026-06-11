using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface ISkinService
{
    Task<SkinResponse> CreateSkinAsync(CreateSkinRequest request);
    Task<IEnumerable<SkinResponse>> GetAllSkinsAsync();
    Task<SkinResponse> GetSkinAsync(int id);
    Task DeactivateSkinAsync(int id);
    Task<PlayerLoadoutResponse> EquipPlayerSkinAsync(int playerId, EquipPlayerSkinRequest request);
    Task<PlayerLoadoutResponse> GetPlayerLoadoutAsync(int playerId);
    Task<TournamentBackgroundResponse> SetTournamentBackgroundAsync(int tournamentId, SetTournamentBackgroundRequest request);
    Task<TournamentBackgroundResponse> GetTournamentBackgroundAsync(int tournamentId);
}
