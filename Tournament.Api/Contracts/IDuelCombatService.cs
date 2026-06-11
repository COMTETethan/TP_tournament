using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Contracts;

/// <summary>
/// Orchestrates a tournament duel as a live combat: starts a combat from a duel's two players,
/// drives it turn by turn, and — when it ends — writes the result back to the duel (outcome +
/// duration), which the score derives from automatically.
/// </summary>
public interface IDuelCombatService
{
    Task<DuelCombatResponse> StartFromDuelAsync(int duelId);
    Task<DuelCombatResponse> GetByDuelAsync(int duelId);
    Task<DuelCombatResponse> SubmitActionAsync(int duelId, SubmitActionRequest request);
    Task<DuelCombatResponse> ForfeitAsync(int duelId, ForfeitRequest request);
    Task<CombatReplayResponse> GetReplayByDuelAsync(int duelId);
}
