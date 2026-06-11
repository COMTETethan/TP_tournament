using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Services;

public class BattlepassService : IBattlepassService
{
    public Task<BattlepassResponse> CreateBattlepassAsync(CreateBattlepassRequest request)
        => throw new NotImplementedException();

    public Task<BattlepassResponse> GetBattlepassBySeasonAsync(int seasonId)
        => throw new NotImplementedException();

    public Task<BattlepassTierResponse> AddTierAsync(int battlepassId, AddBattlepassTierRequest request)
        => throw new NotImplementedException();

    public Task<IEnumerable<BattlepassTierResponse>> GetTiersAsync(int battlepassId)
        => throw new NotImplementedException();

    public Task<PlayerBattlepassProgressResponse> GetPlayerProgressAsync(int battlepassId, int playerId)
        => throw new NotImplementedException();

    public Task<PlayerBattlepassProgressResponse> AddXpAsync(int battlepassId, int playerId, AddXpRequest request)
        => throw new NotImplementedException();
}
