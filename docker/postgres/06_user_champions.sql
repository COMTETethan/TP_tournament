-- =============================================================
--  Tournament — champions owned by users, reusable across tournaments
--  Runs after 05_player_champion.sql.
--  A player is now a user-owned champion (class + level), no longer bound
--  to a single tournament; participation is the tournament_players join,
--  which also carries per-tournament state (disqualification, penalties).
--  Idempotent.
-- =============================================================

-- ── players become user-owned champions ───────────────────────
-- user_id is the owning user's id. No hard FK to users: the auth store can run
-- independently (in-memory) from the champion store, so the column just records
-- the owner from the JWT without coupling the two stores.
ALTER TABLE players ADD COLUMN IF NOT EXISTS user_id INT;

-- A champion always has a class.
ALTER TABLE players ALTER COLUMN class_id SET NOT NULL;

-- Drop the single-tournament binding and per-tournament state (moved to the join).
ALTER TABLE players DROP CONSTRAINT IF EXISTS fk_players_tournament;
ALTER TABLE players DROP CONSTRAINT IF EXISTS chk_penalty_non_negative;
DROP INDEX  IF EXISTS idx_players_tournament;
ALTER TABLE players DROP COLUMN IF EXISTS tournament_id;
ALTER TABLE players DROP COLUMN IF EXISTS is_disqualified;
ALTER TABLE players DROP COLUMN IF EXISTS penalty_points;

CREATE INDEX IF NOT EXISTS idx_players_user ON players(user_id);

-- ── tournament_players: a champion registered in a tournament ──
--  Many-to-many between tournaments and players; holds the per-tournament
--  state (a champion disqualified in one tournament stays active in others).
CREATE TABLE IF NOT EXISTS tournament_players (
    tournament_id   INT         NOT NULL,
    player_id       INT         NOT NULL,
    is_disqualified BOOLEAN     NOT NULL DEFAULT FALSE,
    penalty_points  INT         NOT NULL DEFAULT 0,
    registered_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_tournament_players PRIMARY KEY (tournament_id, player_id),
    CONSTRAINT fk_tp_tournament
        FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE,
    CONSTRAINT fk_tp_player
        FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
    CONSTRAINT chk_tp_penalty_non_negative CHECK (penalty_points >= 0)
);

CREATE INDEX IF NOT EXISTS idx_tp_player ON tournament_players(player_id);
