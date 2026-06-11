using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

public interface ICombatService
{
    Task<CombatResponse> StartCombatAsync(CreateCombatRequest request);
    Task<CombatResponse> GetCombatAsync(int id);
    Task<IEnumerable<CombatResponse>> GetAllCombatsAsync();
    Task<CombatResponse> SubmitActionAsync(int combatId, SubmitActionRequest request);
    Task<CombatResponse> ForfeitAsync(int combatId, ForfeitRequest request);
}
