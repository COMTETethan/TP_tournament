namespace Tournament.Api.Services;

/// <summary>A champion class definition.</summary>
public sealed record ClassDef(int Id, string Name, string Description);

/// <summary>
/// A class skill definition.
/// <para>Category: ATTACK, DEFEND, HEAL or AURA.</para>
/// <para>AuraEffect (AURA only): ATTACK_UP, ATTACK_DOWN, DEFENSE_UP, DEFENSE_DOWN; null otherwise.</para>
/// </summary>
public sealed record SkillDef(
    int Id,
    int ClassId,
    string Name,
    string Category,
    int Power,
    int Duration,
    string? AuraEffect,
    string Description);

/// <summary>
/// Read-only, seeded roster of classes and skills — the single source of truth shared by
/// <see cref="ClassService"/> (read endpoints) and <see cref="CombatService"/> (turn resolution).
/// Each class respects the design rule: 0–3 skills per category, 5 skills maximum.
/// </summary>
public static class ClassCatalog
{
    public static readonly IReadOnlyList<ClassDef> Classes = new List<ClassDef>
    {
        new(1, "Knight",    "A sturdy front-liner balancing offense, defense and self-sustain."),
        new(2, "Mage",      "A glass cannon dealing heavy magical damage but fragile."),
        new(3, "Cleric",    "A support class focused on healing and protective auras."),
        new(4, "Rogue",     "A striker that debuffs enemies and sharpens its own blades."),
        new(5, "Berserker", "A relentless attacker trading all utility for raw damage."),
    };

    public static readonly IReadOnlyList<SkillDef> Skills = new List<SkillDef>
    {
        // ── Knight (2 ATTACK, 1 DEFEND, 1 HEAL, 1 AURA = 5) ──────────────────────
        new( 1, 1, "Sword Slash",  "ATTACK", 25, 0, null,          "A clean strike with the longsword."),
        new( 2, 1, "Shield Bash",  "ATTACK", 18, 0, null,          "Slams the shield into the enemy."),
        new( 3, 1, "Guard",        "DEFEND", 20, 1, null,          "Raises the shield, reducing damage this turn."),
        new( 4, 1, "War Cry",      "AURA",   10, 3, "ATTACK_UP",   "Bolsters the Knight's attack for several turns."),
        new( 5, 1, "Second Wind",  "HEAL",   20, 0, null,          "Catches a breath to recover some health."),

        // ── Mage (3 ATTACK, 1 AURA = 4) ─────────────────────────────────────────
        new( 6, 2, "Fireball",     "ATTACK", 35, 0, null,          "Hurls a searing ball of fire."),
        new( 7, 2, "Ice Shard",    "ATTACK", 22, 0, null,          "Launches a piercing shard of ice."),
        new( 8, 2, "Arcane Blast", "ATTACK", 28, 0, null,          "Unleashes a burst of raw arcane energy."),
        new( 9, 2, "Weaken",       "AURA",   12, 2, "ATTACK_DOWN", "Saps the enemy's strength, lowering its attack."),

        // ── Cleric (1 ATTACK, 1 DEFEND, 2 HEAL, 1 AURA = 5) ─────────────────────
        new(10, 3, "Smite",        "ATTACK", 20, 0, null,          "Calls down holy light on the foe."),
        new(11, 3, "Sanctuary",    "DEFEND", 15, 2, null,          "Surrounds the Cleric with protective light."),
        new(12, 3, "Heal",         "HEAL",   30, 0, null,          "Mends wounds, restoring health."),
        new(13, 3, "Greater Heal", "HEAL",   45, 0, null,          "A powerful prayer restoring much health."),
        new(14, 3, "Bless",        "AURA",   12, 3, "DEFENSE_UP",  "Blesses the Cleric, raising its defense."),

        // ── Rogue (2 ATTACK, 1 DEFEND, 2 AURA = 5) ──────────────────────────────
        new(15, 4, "Backstab",        "ATTACK", 30, 0, null,            "A vicious strike from the shadows."),
        new(16, 4, "Poison Strike",   "ATTACK", 18, 0, null,            "Coats the blade in poison before striking."),
        new(17, 4, "Evasion",         "DEFEND", 18, 1, null,            "Prepares to dodge, reducing incoming damage."),
        new(18, 4, "Expose Weakness", "AURA",   15, 2, "DEFENSE_DOWN",  "Finds a gap in the enemy's guard, lowering its defense."),
        new(19, 4, "Sharpen Blades",  "AURA",   12, 3, "ATTACK_UP",     "Hones the daggers, raising the Rogue's attack."),

        // ── Berserker (3 ATTACK, 1 DEFEND = 4 ; 0 HEAL, 0 AURA) ─────────────────
        new(20, 5, "Reckless Swing", "ATTACK", 32, 0, null,          "A wild, powerful swing of the axe."),
        new(21, 5, "Frenzy",         "ATTACK", 26, 0, null,          "A flurry of frenzied blows."),
        new(22, 5, "Execute",        "ATTACK", 40, 0, null,          "A devastating blow meant to finish the foe."),
        new(23, 5, "Brace",          "DEFEND", 10, 1, null,          "Plants the feet to weather the next hit."),
    };

    public static ClassDef? FindClass(int id) => Classes.FirstOrDefault(c => c.Id == id);

    public static SkillDef? FindSkill(int id) => Skills.FirstOrDefault(s => s.Id == id);

    public static IEnumerable<SkillDef> SkillsForClass(int classId) => Skills.Where(s => s.ClassId == classId);

    public static bool ClassExists(int id) => Classes.Any(c => c.Id == id);
}
