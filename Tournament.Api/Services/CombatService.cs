using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.Services;

/// <summary>
/// Turn-based, RPG-Maker-style combat between two champions.
/// <para>
/// Each turn both champions submit one skill; once both have submitted, the turn is resolved
/// server-side. Resolution happens in two phases: first the defensive/support actions
/// (DEFEND, HEAL, AURA) so they take effect before damage lands, then the attacks — fastest
/// champion first (higher level, then slot 1). A champion knocked out before it acts is skipped.
/// Active buffs/debuffs tick down at the end of each turn.
/// </para>
/// <para>A champion's maximum HP is <c>100 + 10 × level</c>.</para>
/// <para>
/// Every meaningful action is recorded as an ordered <see cref="CombatEventResponse"/> with a
/// snapshot of both champions' HP. When the combat ends (knock-out or forfeit) the stream is
/// finalized; the resulting replay is then served read-only via <see cref="GetReplayAsync"/> so a
/// frontend can play back an old fight without ever having to build it itself.
/// </para>
/// </summary>
public class CombatService : ICombatService
{
    private const string StatusInProgress = "IN_PROGRESS";
    private const string StatusCompleted  = "COMPLETED";

    private static readonly object Lock = new();
    private static readonly List<CombatEntity> Combats = new();
    private static int _nextId = 1;

    public Task<CombatResponse> StartCombatAsync(CreateCombatRequest request)
    {
        var c1 = BuildCombatant(1, request.Champion1);
        var c2 = BuildCombatant(2, request.Champion2);

        lock (Lock)
        {
            var combat = new CombatEntity
            {
                Id         = _nextId++,
                Status     = StatusInProgress,
                Turn       = 1,
                WinnerSlot = null,
                C1         = c1,
                C2         = c2,
                CreatedAt  = DateTime.UtcNow
            };
            RecordEvent(combat, "COMBAT_START",
                $"Combat started: {c1.Name} (Lv{c1.Level}, {c1.MaxHp} HP) vs {c2.Name} (Lv{c2.Level}, {c2.MaxHp} HP).");
            Combats.Add(combat);
            return Task.FromResult(Map(combat));
        }
    }

    public Task<CombatResponse> GetCombatAsync(int id)
    {
        lock (Lock)
        {
            return Task.FromResult(Map(FindOrThrow(id)));
        }
    }

    public Task<IEnumerable<CombatResponse>> GetAllCombatsAsync()
    {
        lock (Lock)
        {
            return Task.FromResult<IEnumerable<CombatResponse>>(Combats.Select(Map).ToList());
        }
    }

    public Task<CombatResponse> SubmitActionAsync(int combatId, SubmitActionRequest request)
    {
        lock (Lock)
        {
            var combat = FindOrThrow(combatId);

            if (combat.Status == StatusCompleted)
                throw new InvalidCombatActionException("Combat is already over.");

            var actor = request.Slot switch
            {
                1 => combat.C1,
                2 => combat.C2,
                _ => throw new InvalidCombatActionException($"Invalid slot {request.Slot}; expected 1 or 2.")
            };

            var skill = ClassCatalog.FindSkill(request.SkillId)
                        ?? throw new SkillNotFoundException(request.SkillId);

            if (skill.ClassId != actor.ClassId)
                throw new InvalidCombatActionException(
                    $"Skill {request.SkillId} does not belong to {actor.Name}'s class.");

            if (actor.HasSubmitted)
                throw new InvalidCombatActionException(
                    $"{actor.Name} has already submitted an action this turn.");

            actor.PendingSkillId = request.SkillId;
            actor.HasSubmitted   = true;

            if (combat.C1.HasSubmitted && combat.C2.HasSubmitted)
                ResolveTurn(combat);

            return Task.FromResult(Map(combat));
        }
    }

    public Task<CombatResponse> ForfeitAsync(int combatId, ForfeitRequest request)
    {
        lock (Lock)
        {
            var combat = FindOrThrow(combatId);

            if (combat.Status == StatusCompleted)
                throw new InvalidCombatActionException("Combat is already over.");

            var (quitter, winner) = request.Slot switch
            {
                1 => (combat.C1, 2),
                2 => (combat.C2, 1),
                _ => throw new InvalidCombatActionException($"Invalid slot {request.Slot}; expected 1 or 2.")
            };

            Finish(combat, winner);
            RecordEvent(combat, "FORFEIT",
                $"{quitter.Name} forfeits. Champion {winner} wins!", actor: quitter);
            return Task.FromResult(Map(combat));
        }
    }

    public Task<CombatReplayResponse> GetReplayAsync(int id)
    {
        lock (Lock)
        {
            return Task.FromResult(MapReplay(FindOrThrow(id)));
        }
    }

    // ── Turn resolution ──────────────────────────────────────────────────────────

    private static void ResolveTurn(CombatEntity combat)
    {
        var c1 = combat.C1;
        var c2 = combat.C2;
        var s1 = ClassCatalog.FindSkill(c1.PendingSkillId!.Value)!;
        var s2 = ClassCatalog.FindSkill(c2.PendingSkillId!.Value)!;

        // Phase 1 — defensive / support actions land before attacks.
        ApplyNonAttack(combat, c1, c2, s1);
        ApplyNonAttack(combat, c2, c1, s2);

        // Phase 2 — attacks, fastest champion first; a downed attacker cannot act.
        foreach (var (attacker, defender, skill) in AttackOrder(combat, s1, s2))
        {
            if (attacker.CurrentHp <= 0)
            {
                RecordEvent(combat, "SKIPPED", $"{attacker.Name} is down and cannot act.", actor: attacker);
                continue;
            }
            ApplyAttack(combat, attacker, defender, skill);
        }

        TickEffects(c1);
        TickEffects(c2);
        ClearSubmission(c1);
        ClearSubmission(c2);

        if (c1.CurrentHp <= 0 || c2.CurrentHp <= 0)
        {
            var winner = c1.CurrentHp <= 0 ? 2 : 1;
            Finish(combat, winner);
            var champ = winner == 1 ? c1 : c2;
            RecordEvent(combat, "VICTORY", $"Combat over — {champ.Name} wins!", actor: champ);
        }
        else
        {
            combat.Turn++;
        }
    }

    private static IEnumerable<(CombatantEntity attacker, CombatantEntity defender, SkillDef skill)>
        AttackOrder(CombatEntity combat, SkillDef s1, SkillDef s2)
    {
        var actions = new List<(CombatantEntity attacker, CombatantEntity defender, SkillDef skill)>();
        if (s1.Category == "ATTACK") actions.Add((combat.C1, combat.C2, s1));
        if (s2.Category == "ATTACK") actions.Add((combat.C2, combat.C1, s2));
        return actions
            .OrderByDescending(a => a.attacker.Level)
            .ThenBy(a => a.attacker.Slot)
            .ToList();
    }

    private static void ApplyNonAttack(CombatEntity combat, CombatantEntity actor, CombatantEntity enemy, SkillDef skill)
    {
        switch (skill.Category)
        {
            case "DEFEND":
                actor.Effects.Add(new EffectEntity
                {
                    EffectType     = "DEFENSE_UP",
                    Magnitude      = skill.Power,
                    RemainingTurns = Math.Max(1, skill.Duration)
                });
                RecordEvent(combat, "DEFEND",
                    $"{actor.Name} uses {skill.Name}, raising defense by {skill.Power}.",
                    actor: actor, skill: skill, target: actor, amount: skill.Power);
                break;

            case "HEAL":
                var healed = Math.Min(skill.Power, actor.MaxHp - actor.CurrentHp);
                actor.CurrentHp += healed;
                RecordEvent(combat, "HEAL",
                    $"{actor.Name} uses {skill.Name}, healing {healed} HP. " +
                    $"({actor.Name}: {actor.CurrentHp}/{actor.MaxHp} HP)",
                    actor: actor, skill: skill, target: actor, amount: healed);
                break;

            case "AURA":
                ApplyAura(combat, actor, enemy, skill);
                break;
        }
    }

    private static void ApplyAura(CombatEntity combat, CombatantEntity actor, CombatantEntity enemy, SkillDef skill)
    {
        var effect   = skill.AuraEffect!;
        var duration = Math.Max(1, skill.Duration);
        var target   = effect.EndsWith("_UP") ? actor : enemy; // buffs on self, debuffs on the enemy
        target.Effects.Add(new EffectEntity
        {
            EffectType     = effect,
            Magnitude      = skill.Power,
            RemainingTurns = duration
        });
        RecordEvent(combat, "AURA",
            $"{actor.Name} uses {skill.Name}: {effect} {skill.Power} on {target.Name} for {duration} turn(s).",
            actor: actor, skill: skill, target: target, amount: skill.Power, effect: effect);
    }

    private static void ApplyAttack(CombatEntity combat, CombatantEntity attacker, CombatantEntity defender, SkillDef skill)
    {
        var raw    = skill.Power + AttackBonus(attacker) - Defense(defender);
        var damage = Math.Max(1, raw);
        defender.CurrentHp = Math.Max(0, defender.CurrentHp - damage);
        RecordEvent(combat, "ATTACK",
            $"{attacker.Name} uses {skill.Name} on {defender.Name} for {damage} damage. " +
            $"({defender.Name}: {defender.CurrentHp}/{defender.MaxHp} HP)",
            actor: attacker, skill: skill, target: defender, amount: damage);
    }

    private static int AttackBonus(CombatantEntity c)
        => c.Effects.Where(e => e.EffectType == "ATTACK_UP").Sum(e => e.Magnitude)
         - c.Effects.Where(e => e.EffectType == "ATTACK_DOWN").Sum(e => e.Magnitude);

    private static int Defense(CombatantEntity c)
        => c.Effects.Where(e => e.EffectType == "DEFENSE_UP").Sum(e => e.Magnitude)
         - c.Effects.Where(e => e.EffectType == "DEFENSE_DOWN").Sum(e => e.Magnitude);

    private static void TickEffects(CombatantEntity c)
    {
        foreach (var e in c.Effects) e.RemainingTurns--;
        c.Effects.RemoveAll(e => e.RemainingTurns <= 0);
    }

    private static void ClearSubmission(CombatantEntity c)
    {
        c.PendingSkillId = null;
        c.HasSubmitted   = false;
    }

    private static void Finish(CombatEntity combat, int winnerSlot)
    {
        combat.Status      = StatusCompleted;
        combat.WinnerSlot  = winnerSlot;
        combat.CompletedAt = DateTime.UtcNow;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static CombatantEntity BuildCombatant(int slot, CombatantSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Name))
            throw new ArgumentException("Champion name cannot be empty.", nameof(spec));
        if (spec.Level < 1)
            throw new ArgumentException("Champion level must be at least 1.", nameof(spec));
        if (!ClassCatalog.ClassExists(spec.ClassId))
            throw new ClassNotFoundException(spec.ClassId);

        var maxHp = 100 + 10 * spec.Level;
        return new CombatantEntity
        {
            Slot      = slot,
            Name      = spec.Name,
            ClassId   = spec.ClassId,
            Level     = spec.Level,
            MaxHp     = maxHp,
            CurrentHp = maxHp
        };
    }

    private static CombatEntity FindOrThrow(int id)
        => Combats.FirstOrDefault(c => c.Id == id) ?? throw new CombatNotFoundException(id);

    /// <summary>Appends a structured replay event (with an HP snapshot) and its narrative log line.</summary>
    private static void RecordEvent(CombatEntity combat, string type, string message,
        CombatantEntity? actor = null, SkillDef? skill = null, CombatantEntity? target = null,
        int? amount = null, string? effect = null)
    {
        combat.Events.Add(new CombatEventEntity
        {
            Sequence    = combat.Events.Count + 1,
            Turn        = combat.Turn,
            Type        = type,
            ActorSlot   = actor?.Slot,
            SkillId     = skill?.Id,
            SkillName   = skill?.Name,
            Category    = skill?.Category,
            TargetSlot  = target?.Slot,
            Amount      = amount,
            Effect      = effect,
            Champion1Hp = combat.C1.CurrentHp,
            Champion2Hp = combat.C2.CurrentHp,
            Message     = message
        });
        combat.Log.Add(new CombatLogEntry(combat.Turn, message));
    }

    private static CombatResponse Map(CombatEntity c)
        => new(c.Id, c.Status, c.Turn, c.WinnerSlot,
               MapCombatant(c.C1), MapCombatant(c.C2),
               c.Log.ToList(), c.CreatedAt);

    private static CombatantState MapCombatant(CombatantEntity c)
        => new(c.Slot, c.Name, c.ClassId, c.Level, c.MaxHp, c.CurrentHp, c.HasSubmitted,
               c.Effects.Select(e => new ActiveEffectState(e.EffectType, e.Magnitude, e.RemainingTurns)).ToList());

    private static CombatReplayResponse MapReplay(CombatEntity c)
        => new(c.Id, c.Status, c.WinnerSlot, c.Turn,
               MapReplayChampion(c.C1), MapReplayChampion(c.C2),
               c.CreatedAt, c.CompletedAt,
               c.Events.Select(MapEvent).ToList());

    private static ReplayChampion MapReplayChampion(CombatantEntity c)
        // The class id is validated when the combatant is built, so the lookup always succeeds.
        => new(c.Slot, c.Name, c.ClassId, ClassCatalog.FindClass(c.ClassId)!.Name, c.Level, c.MaxHp);

    private static CombatEventResponse MapEvent(CombatEventEntity e)
        => new(e.Sequence, e.Turn, e.Type, e.ActorSlot, e.SkillId, e.SkillName, e.Category,
               e.TargetSlot, e.Amount, e.Effect, e.Champion1Hp, e.Champion2Hp, e.Message);

    // ── In-memory entities ───────────────────────────────────────────────────────

    private class CombatEntity
    {
        public int Id { get; set; }
        public string Status { get; set; } = StatusInProgress;
        public int Turn { get; set; }
        public int? WinnerSlot { get; set; }
        public CombatantEntity C1 { get; set; } = null!;
        public CombatantEntity C2 { get; set; } = null!;
        public List<CombatLogEntry> Log { get; } = new();
        public List<CombatEventEntity> Events { get; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    private class CombatantEntity
    {
        public int Slot { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public int Level { get; set; }
        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }
        public int? PendingSkillId { get; set; }
        public bool HasSubmitted { get; set; }
        public List<EffectEntity> Effects { get; } = new();
    }

    private class EffectEntity
    {
        public string EffectType { get; set; } = string.Empty;
        public int Magnitude { get; set; }
        public int RemainingTurns { get; set; }
    }

    private class CombatEventEntity
    {
        public int Sequence { get; set; }
        public int Turn { get; set; }
        public string Type { get; set; } = string.Empty;
        public int? ActorSlot { get; set; }
        public int? SkillId { get; set; }
        public string? SkillName { get; set; }
        public string? Category { get; set; }
        public int? TargetSlot { get; set; }
        public int? Amount { get; set; }
        public string? Effect { get; set; }
        public int Champion1Hp { get; set; }
        public int Champion2Hp { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
