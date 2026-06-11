-- =============================================================
--  Tournament — link players to the combat module (Phase 4)
--  A player with a class becomes a fightable champion.
--  Runs after 04_combat.sql (the classes table must already exist).
--  Idempotent.
-- =============================================================

ALTER TABLE players ADD COLUMN IF NOT EXISTS class_id INT;
ALTER TABLE players ADD COLUMN IF NOT EXISTS level    INT NOT NULL DEFAULT 1;

DO $$ BEGIN
    ALTER TABLE players
        ADD CONSTRAINT fk_players_class
        FOREIGN KEY (class_id) REFERENCES classes(id) ON DELETE SET NULL;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    ALTER TABLE players
        ADD CONSTRAINT chk_players_level_positive CHECK (level >= 1);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

CREATE INDEX IF NOT EXISTS idx_players_class ON players(class_id) WHERE class_id IS NOT NULL;
