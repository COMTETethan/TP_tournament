using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

/// <summary>
/// Bridges a tournament duel and the combat engine. A duel's two players (which must have a class)
/// fight as champions; when the combat ends the result is written back to the duel — outcome
/// (PLAYER1_WIN / PLAYER2_WIN) and duration — from which the tournament score is derived.
/// </summary>
public class DuelCombatService : IDuelCombatService
{
    // Instance state: the service is a Singleton, so the link persists across requests.
    private readonly object _lock = new();
    private readonly Dictionary<int, int> _duelToCombat = new(); // duelId → combatId

    private readonly IDuelService _duels;
    private readonly IPlayerService _players;
    private readonly ICombatService _combat;

    public DuelCombatService()
        : this(new DuelService(), new PlayerService(), new CombatService()) { }

    public DuelCombatService(IDuelService duels, IPlayerService players, ICombatService combat)
    {
        _duels   = duels;
        _players = players;
        _combat  = combat;
    }

    public async Task<DuelCombatResponse> StartFromDuelAsync(int duelId)
    {
        var duel = await _duels.GetDuelAsync(duelId); // throws DuelNotFoundException

        lock (_lock)
        {
            if (_duelToCombat.ContainsKey(duelId))
                throw new InvalidCombatActionException($"A combat has already been started for duel {duelId}.");
        }

        if (duel.Outcome is not null)
            throw new InvalidCombatActionException($"Duel {duelId} already has an outcome and cannot be fought again.");

        var p1 = await _players.GetPlayerAsync(duel.Player1Id); // throws PlayerNotFoundException
        var p2 = await _players.GetPlayerAsync(duel.Player2Id);

        var combat = await _combat.StartCombatAsync(new CreateCombatRequest(ToSpec(p1), ToSpec(p2)));

        lock (_lock) { _duelToCombat[duelId] = combat.Id; }

        return Map(duel, combat);
    }

    public async Task<DuelCombatResponse> GetByDuelAsync(int duelId)
    {
        var combatId = await ResolveCombatIdAsync(duelId);
        var combat   = await _combat.GetCombatAsync(combatId);
        var duel     = await MaybeFinalizeAsync(duelId, combat);
        return Map(duel, combat);
    }

    public async Task<DuelCombatResponse> SubmitActionAsync(int duelId, SubmitActionRequest request)
    {
        var combatId = await ResolveCombatIdAsync(duelId);
        var combat   = await _combat.SubmitActionAsync(combatId, request);
        var duel     = await MaybeFinalizeAsync(duelId, combat);
        return Map(duel, combat);
    }

    public async Task<DuelCombatResponse> ForfeitAsync(int duelId, ForfeitRequest request)
    {
        var combatId = await ResolveCombatIdAsync(duelId);
        var combat   = await _combat.ForfeitAsync(combatId, request);
        var duel     = await MaybeFinalizeAsync(duelId, combat);
        return Map(duel, combat);
    }

    public async Task<CombatReplayResponse> GetReplayByDuelAsync(int duelId)
    {
        var combatId = await ResolveCombatIdAsync(duelId);
        return await _combat.GetReplayAsync(combatId);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private async Task<int> ResolveCombatIdAsync(int duelId)
    {
        lock (_lock)
        {
            if (_duelToCombat.TryGetValue(duelId, out var combatId))
                return combatId;
        }
        await _duels.GetDuelAsync(duelId); // surfaces DuelNotFoundException for an unknown duel
        throw new InvalidCombatActionException($"No combat has been started for duel {duelId}. Start one first.");
    }

    /// <summary>
    /// When the combat is over, write the result back to the duel exactly once: set the outcome
    /// (which the score derives from) and end the duel. Idempotent.
    /// </summary>
    private async Task<DuelResponse> MaybeFinalizeAsync(int duelId, CombatResponse combat)
    {
        var duel = await _duels.GetDuelAsync(duelId);
        if (combat.Status != "COMPLETED" || duel.Outcome is not null)
            return duel;

        var outcome = combat.WinnerSlot == 1 ? "PLAYER1_WIN" : "PLAYER2_WIN";
        await _duels.SetDuelOutcomeAsync(duelId, new SetDuelOutcomeRequest(outcome));
        try
        {
            // DurationSeconds stands in for how long the fight lasted: the number of turns played.
            await _duels.EndDuelAsync(duelId, new EndDuelRequest(combat.Turn));
        }
        catch (DuelAlreadyEndedException) { /* already ended elsewhere; the outcome is what matters */ }

        return await _duels.GetDuelAsync(duelId);
    }

    private static CombatantSpec ToSpec(PlayerResponse p)
    {
        if (p.ClassId is null)
            throw new InvalidCombatActionException($"Player {p.Id} ({p.Name}) has no class assigned and cannot fight.");
        return new CombatantSpec(p.Name, p.ClassId.Value, p.Level);
    }

    private static DuelCombatResponse Map(DuelResponse duel, CombatResponse combat)
    {
        int? winnerPlayerId = combat.WinnerSlot switch
        {
            1 => duel.Player1Id,
            2 => duel.Player2Id,
            _ => null
        };
        return new DuelCombatResponse(
            duel.Id, duel.TournamentId, combat.Id, combat.Status,
            combat.WinnerSlot, winnerPlayerId, duel.Outcome, combat);
    }
}
