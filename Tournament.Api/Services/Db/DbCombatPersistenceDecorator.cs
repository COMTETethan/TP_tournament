using Dapper;
using Npgsql;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;

namespace Tournament.Api.Services.Db;

/// <summary>
/// Wraps the in-memory combat engine and writes the combat through to PostgreSQL after each
/// mutating operation (combats, combatants and the replay event stream). Reads are served from
/// the inner engine; the database is the durable record of every fight.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class DbCombatPersistenceDecorator : ICombatService
{
    private readonly ICombatService _inner;
    private readonly NpgsqlDataSource _db;

    public DbCombatPersistenceDecorator(ICombatService inner, NpgsqlDataSource db)
    {
        _inner = inner;
        _db = db;
    }

    public async Task<CombatResponse> StartCombatAsync(CreateCombatRequest request)
    {
        var combat = await _inner.StartCombatAsync(request);
        await PersistAsync(combat.Id);
        return combat;
    }

    public async Task<CombatResponse> SubmitActionAsync(int combatId, SubmitActionRequest request)
    {
        var combat = await _inner.SubmitActionAsync(combatId, request);
        await PersistAsync(combatId);
        return combat;
    }

    public async Task<CombatResponse> ForfeitAsync(int combatId, ForfeitRequest request)
    {
        var combat = await _inner.ForfeitAsync(combatId, request);
        await PersistAsync(combatId);
        return combat;
    }

    public Task<CombatResponse> GetCombatAsync(int id) => _inner.GetCombatAsync(id);
    public Task<IEnumerable<CombatResponse>> GetAllCombatsAsync() => _inner.GetAllCombatsAsync();
    public Task<CombatReplayResponse> GetReplayAsync(int id) => _inner.GetReplayAsync(id);

    private async Task PersistAsync(int combatId)
    {
        var combat = await _inner.GetCombatAsync(combatId);
        var replay = await _inner.GetReplayAsync(combatId);

        await using var conn = await _db.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();

        await conn.ExecuteAsync(
            @"INSERT INTO combats (id, status, turn, winner_slot, created_at, completed_at)
              VALUES (@Id, @Status::combat_status, @Turn, @WinnerSlot, @CreatedAt, @CompletedAt)
              ON CONFLICT (id) DO UPDATE SET
                status = EXCLUDED.status, turn = EXCLUDED.turn,
                winner_slot = EXCLUDED.winner_slot, completed_at = EXCLUDED.completed_at",
            new { combat.Id, combat.Status, combat.Turn, combat.WinnerSlot, combat.CreatedAt, replay.CompletedAt }, tx);

        foreach (var c in new[] { combat.Champion1, combat.Champion2 })
        {
            await conn.ExecuteAsync(
                @"INSERT INTO combatants (combat_id, slot, name, class_id, level, current_hp, has_submitted)
                  VALUES (@CombatId, @Slot, @Name, @ClassId, @Level, @CurrentHp, @HasSubmitted)
                  ON CONFLICT (combat_id, slot) DO UPDATE SET
                    current_hp = EXCLUDED.current_hp, has_submitted = EXCLUDED.has_submitted",
                new { CombatId = combat.Id, c.Slot, c.Name, c.ClassId, c.Level, c.CurrentHp, HasSubmitted = c.HasSubmittedAction }, tx);
        }

        // Delete stale events before re-inserting the full replay so that a restarted
        // API (which resets the in-memory id counter to 1) never leaves orphaned events
        // from a previous session mixed into the new combat's replay stream.
        await conn.ExecuteAsync(
            "DELETE FROM combat_events WHERE combat_id = @Id",
            new { combat.Id }, tx);

        foreach (var e in replay.Events)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO combat_events
                    (combat_id, sequence, turn, event_type, actor_slot, skill_id, target_slot, amount, effect, champion1_hp, champion2_hp, message)
                  VALUES (@CombatId, @Sequence, @Turn, @Type, @ActorSlot, @SkillId, @TargetSlot, @Amount, @Effect::aura_effect, @Champion1Hp, @Champion2Hp, @Message)",
                new { CombatId = combat.Id, e.Sequence, e.Turn, e.Type, e.ActorSlot, e.SkillId,
                      e.TargetSlot, e.Amount, e.Effect, e.Champion1Hp, e.Champion2Hp, e.Message }, tx);
        }

        await tx.CommitAsync();
    }
}
