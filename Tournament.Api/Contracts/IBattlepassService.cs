using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface IBattlepassService
{
    Task<BattlepassResponse> CreateBattlepassAsync(CreateBattlepassRequest request);
    Task<BattlepassResponse> GetBattlepassBySeasonAsync(int seasonId);
    Task<BattlepassTierResponse> AddTierAsync(int battlepassId, AddBattlepassTierRequest request);
    Task<IEnumerable<BattlepassTierResponse>> GetTiersAsync(int battlepassId);
    Task<PlayerBattlepassProgressResponse> GetPlayerProgressAsync(int battlepassId, int playerId);
    Task<PlayerBattlepassProgressResponse> AddXpAsync(int battlepassId, int playerId, AddXpRequest request);
}
