-- =============================================================
--  Tournament — PostgreSQL combat module (Phase 4)
--  Classes, skills and turn-based combat between champions.
--  Executed automatically on container start.
--  Idempotent: all objects use IF NOT EXISTS / ON CONFLICT.
-- =============================================================

-- ── Enum types ────────────────────────────────────────────────

DO $$ BEGIN
    CREATE TYPE skill_category AS ENUM ('ATTACK', 'DEFEND', 'HEAL', 'AURA');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE aura_effect AS ENUM ('ATTACK_UP', 'ATTACK_DOWN', 'DEFENSE_UP', 'DEFENSE_DOWN');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE combat_status AS ENUM ('IN_PROGRESS', 'COMPLETED');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- ── classes ───────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS classes (
    id          SERIAL       NOT NULL,
    name        VARCHAR(50)  NOT NULL,
    description VARCHAR(255) NOT NULL DEFAULT '',
    CONSTRAINT pk_classes   PRIMARY KEY (id),
    CONSTRAINT uq_classes_name UNIQUE (name)
);

-- ── skills ────────────────────────────────────────────────────
--  power       : damage (ATTACK), shield (DEFEND), HP restored (HEAL) or effect strength (AURA)
--  duration    : how many turns an applied effect lasts (0 for instant skills)
--  aura_effect : required for AURA skills, NULL for every other category

CREATE TABLE IF NOT EXISTS skills (
    id          SERIAL         NOT NULL,
    class_id    INT            NOT NULL,
    name        VARCHAR(80)    NOT NULL,
    category    skill_category NOT NULL,
    power       INT            NOT NULL DEFAULT 0,
    duration    INT            NOT NULL DEFAULT 0,
    aura_effect aura_effect,
    description VARCHAR(255)   NOT NULL DEFAULT '',
    CONSTRAINT pk_skills PRIMARY KEY (id),
    CONSTRAINT fk_skills_class
        FOREIGN KEY (class_id) REFERENCES classes(id) ON DELETE CASCADE,
    CONSTRAINT chk_skills_power_non_negative    CHECK (power >= 0),
    CONSTRAINT chk_skills_duration_non_negative CHECK (duration >= 0),
    -- AURA skills carry an effect; all other categories must not.
    CONSTRAINT chk_skills_aura_effect CHECK (
        (category =  'AURA' AND aura_effect IS NOT NULL) OR
        (category <> 'AURA' AND aura_effect IS NULL)
    )
);

CREATE INDEX IF NOT EXISTS idx_skills_class ON skills(class_id);

-- Enforce the design rule: at most 3 skills per category and 5 skills total per class.
CREATE OR REPLACE FUNCTION trg_enforce_skill_limits()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
DECLARE
    total_count    INT;
    category_count INT;
BEGIN
    SELECT COUNT(*) INTO total_count
        FROM skills WHERE class_id = NEW.class_id;
    IF total_count >= 5 THEN
        RAISE EXCEPTION 'Class % already has 5 skills (maximum reached).', NEW.class_id;
    END IF;

    SELECT COUNT(*) INTO category_count
        FROM skills WHERE class_id = NEW.class_id AND category = NEW.category;
    IF category_count >= 3 THEN
        RAISE EXCEPTION 'Class % already has 3 % skills (maximum per category).',
            NEW.class_id, NEW.category;
    END IF;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS enforce_skill_limits ON skills;
CREATE TRIGGER enforce_skill_limits
    BEFORE INSERT ON skills
    FOR EACH ROW EXECUTE FUNCTION trg_enforce_skill_limits();

-- ── combats ───────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS combats (
    id          SERIAL        NOT NULL,
    status      combat_status NOT NULL DEFAULT 'IN_PROGRESS',
    turn        INT           NOT NULL DEFAULT 1,
    winner_slot INT,                                   -- NULL until the combat ends
    created_at  TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_combats PRIMARY KEY (id),
    CONSTRAINT chk_combats_turn_positive CHECK (turn >= 1),
    CONSTRAINT chk_combats_winner_slot   CHECK (winner_slot IS NULL OR winner_slot IN (1, 2))
);

-- ── combatants ────────────────────────────────────────────────
--  One row per champion (slot 1 / slot 2) participating in a combat.
--  max_hp is derived from the level: 100 + 10 × level.

CREATE TABLE IF NOT EXISTS combatants (
    id               SERIAL       NOT NULL,
    combat_id        INT          NOT NULL,
    slot             INT          NOT NULL,
    name             VARCHAR(100) NOT NULL,
    class_id         INT          NOT NULL,
    level            INT          NOT NULL DEFAULT 1,
    max_hp           INT          GENERATED ALWAYS AS (100 + 10 * level) STORED,
    current_hp       INT          NOT NULL,
    pending_skill_id INT,                              -- skill chosen for the current turn (NULL = not submitted)
    has_submitted    BOOLEAN      NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_combatants PRIMARY KEY (id),
    CONSTRAINT fk_combatants_combat
        FOREIGN KEY (combat_id) REFERENCES combats(id) ON DELETE CASCADE,
    CONSTRAINT fk_combatants_class
        FOREIGN KEY (class_id) REFERENCES classes(id),
    CONSTRAINT fk_combatants_pending_skill
        FOREIGN KEY (pending_skill_id) REFERENCES skills(id),
    CONSTRAINT chk_combatants_slot           CHECK (slot IN (1, 2)),
    CONSTRAINT chk_combatants_level_positive CHECK (level >= 1),
    CONSTRAINT chk_combatants_hp_non_negative CHECK (current_hp >= 0),
    CONSTRAINT uq_combatants_combat_slot     UNIQUE (combat_id, slot)
);

CREATE INDEX IF NOT EXISTS idx_combatants_combat ON combatants(combat_id);

-- ── combat_effects ────────────────────────────────────────────
--  Active buffs/debuffs on a combatant. effect_type reuses the aura_effect enum;
--  DEFEND skills add a DEFENSE_UP effect.

CREATE TABLE IF NOT EXISTS combat_effects (
    id              SERIAL      NOT NULL,
    combatant_id    INT         NOT NULL,
    effect_type     aura_effect NOT NULL,
    magnitude       INT         NOT NULL,
    remaining_turns INT         NOT NULL,
    CONSTRAINT pk_combat_effects PRIMARY KEY (id),
    CONSTRAINT fk_combat_effects_combatant
        FOREIGN KEY (combatant_id) REFERENCES combatants(id) ON DELETE CASCADE,
    CONSTRAINT chk_combat_effects_remaining CHECK (remaining_turns >= 0)
);

CREATE INDEX IF NOT EXISTS idx_combat_effects_combatant ON combat_effects(combatant_id);

-- ── combat_log ────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS combat_log (
    id         SERIAL       NOT NULL,
    combat_id  INT          NOT NULL,
    turn       INT          NOT NULL,
    message    VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_combat_log PRIMARY KEY (id),
    CONSTRAINT fk_combat_log_combat
        FOREIGN KEY (combat_id) REFERENCES combats(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_combat_log_combat ON combat_log(combat_id, id);

-- =============================================================
--  Seed: classes and their skills (read-only roster)
--  Mirrors Tournament.Api/Services/ClassCatalog.cs
-- =============================================================

INSERT INTO classes (id, name, description) VALUES
    (1, 'Knight',    'A sturdy front-liner balancing offense, defense and self-sustain.'),
    (2, 'Mage',      'A glass cannon dealing heavy magical damage but fragile.'),
    (3, 'Cleric',    'A support class focused on healing and protective auras.'),
    (4, 'Rogue',     'A striker that debuffs enemies and sharpens its own blades.'),
    (5, 'Berserker', 'A relentless attacker trading all utility for raw damage.')
ON CONFLICT (id) DO NOTHING;

INSERT INTO skills (id, class_id, name, category, power, duration, aura_effect, description) VALUES
    -- Knight
    ( 1, 1, 'Sword Slash',     'ATTACK', 25, 0, NULL,           'A clean strike with the longsword.'),
    ( 2, 1, 'Shield Bash',     'ATTACK', 18, 0, NULL,           'Slams the shield into the enemy.'),
    ( 3, 1, 'Guard',           'DEFEND', 20, 1, NULL,           'Raises the shield, reducing damage this turn.'),
    ( 4, 1, 'War Cry',         'AURA',   10, 3, 'ATTACK_UP',    'Bolsters the Knight''s attack for several turns.'),
    ( 5, 1, 'Second Wind',     'HEAL',   20, 0, NULL,           'Catches a breath to recover some health.'),
    -- Mage
    ( 6, 2, 'Fireball',        'ATTACK', 35, 0, NULL,           'Hurls a searing ball of fire.'),
    ( 7, 2, 'Ice Shard',       'ATTACK', 22, 0, NULL,           'Launches a piercing shard of ice.'),
    ( 8, 2, 'Arcane Blast',    'ATTACK', 28, 0, NULL,           'Unleashes a burst of raw arcane energy.'),
    ( 9, 2, 'Weaken',          'AURA',   12, 2, 'ATTACK_DOWN',  'Saps the enemy''s strength, lowering its attack.'),
    -- Cleric
    (10, 3, 'Smite',           'ATTACK', 20, 0, NULL,           'Calls down holy light on the foe.'),
    (11, 3, 'Sanctuary',       'DEFEND', 15, 2, NULL,           'Surrounds the Cleric with protective light.'),
    (12, 3, 'Heal',            'HEAL',   30, 0, NULL,           'Mends wounds, restoring health.'),
    (13, 3, 'Greater Heal',    'HEAL',   45, 0, NULL,           'A powerful prayer restoring much health.'),
    (14, 3, 'Bless',           'AURA',   12, 3, 'DEFENSE_UP',   'Blesses the Cleric, raising its defense.'),
    -- Rogue
    (15, 4, 'Backstab',        'ATTACK', 30, 0, NULL,           'A vicious strike from the shadows.'),
    (16, 4, 'Poison Strike',   'ATTACK', 18, 0, NULL,           'Coats the blade in poison before striking.'),
    (17, 4, 'Evasion',         'DEFEND', 18, 1, NULL,           'Prepares to dodge, reducing incoming damage.'),
    (18, 4, 'Expose Weakness', 'AURA',   15, 2, 'DEFENSE_DOWN', 'Finds a gap in the enemy''s guard, lowering its defense.'),
    (19, 4, 'Sharpen Blades',  'AURA',   12, 3, 'ATTACK_UP',    'Hones the daggers, raising the Rogue''s attack.'),
    -- Berserker
    (20, 5, 'Reckless Swing',  'ATTACK', 32, 0, NULL,           'A wild, powerful swing of the axe.'),
    (21, 5, 'Frenzy',          'ATTACK', 26, 0, NULL,           'A flurry of frenzied blows.'),
    (22, 5, 'Execute',         'ATTACK', 40, 0, NULL,           'A devastating blow meant to finish the foe.'),
    (23, 5, 'Brace',           'DEFEND', 10, 1, NULL,           'Plants the feet to weather the next hit.')
ON CONFLICT (id) DO NOTHING;

-- Keep the SERIAL sequences in sync with the explicit ids inserted above.
SELECT setval('classes_id_seq', (SELECT MAX(id) FROM classes));
SELECT setval('skills_id_seq',  (SELECT MAX(id) FROM skills));
