-- =============================================================
--  Tournament — PostgreSQL initialisation (Phase 1)
--  Executed automatically on first container start.
--  Idempotent: all objects use IF NOT EXISTS.
-- =============================================================

-- ── tournaments ───────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS tournaments (
    id         SERIAL       NOT NULL,
    name       VARCHAR(150) NOT NULL,
    status     VARCHAR(20)  NOT NULL DEFAULT 'OPEN',
    created_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_tournaments PRIMARY KEY (id),
    CONSTRAINT chk_tournament_status
        CHECK (status IN ('OPEN', 'IN_PROGRESS', 'CLOSED'))
);

-- ── players ───────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS players (
    id              SERIAL       NOT NULL,
    tournament_id   INT          NOT NULL,
    name            VARCHAR(100) NOT NULL,
    is_disqualified BOOLEAN      NOT NULL DEFAULT FALSE,
    penalty_points  INT          NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_players          PRIMARY KEY (id),
    CONSTRAINT fk_players_tournament
        FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE,
    CONSTRAINT chk_penalty_non_negative
        CHECK (penalty_points >= 0)
);

-- ── duels ─────────────────────────────────────────────────────
--  played_at : when the duel started (UTC)
--  duration  : how long the duel lasted  (INTERVAL, e.g. '00:04:37')
--              NULL if the duel has not yet ended

CREATE TABLE IF NOT EXISTS duels (
    id            SERIAL        NOT NULL,
    tournament_id INT           NOT NULL,
    player1_id    INT           NOT NULL,
    player2_id    INT           NOT NULL,
    outcome       VARCHAR(20),                   -- NULL while the duel is in progress
    duel_order    INT           NOT NULL,
    played_at     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    duration      INTERVAL,                      -- NULL until the duel ends
    CONSTRAINT pk_duels PRIMARY KEY (id),
    CONSTRAINT fk_duels_tournament
        FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE,
    CONSTRAINT fk_duels_player1
        FOREIGN KEY (player1_id) REFERENCES players(id),
    CONSTRAINT fk_duels_player2
        FOREIGN KEY (player2_id) REFERENCES players(id),
    CONSTRAINT chk_different_players
        CHECK (player1_id <> player2_id),
    CONSTRAINT chk_duel_outcome
        CHECK (outcome IN ('PLAYER1_WIN', 'PLAYER2_WIN', 'DRAW') OR outcome IS NULL)
);

-- Computed column helper (PostgreSQL >= 12): end time of the duel
-- SELECT id, played_at, duration, played_at + duration AS ended_at FROM duels;

-- ── match_results ─────────────────────────────────────────────
--  One row per player per duel.
--  match_order mirrors duel_order — used to sort results chronologically
--  when feeding ScoreCalculator.

CREATE TABLE IF NOT EXISTS match_results (
    id          SERIAL        NOT NULL,
    player_id   INT           NOT NULL,
    duel_id     INT           NOT NULL,
    outcome     VARCHAR(10)   NOT NULL,
    match_order INT           NOT NULL,
    CONSTRAINT pk_match_results PRIMARY KEY (id),
    CONSTRAINT fk_mr_player
        FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
    CONSTRAINT fk_mr_duel
        FOREIGN KEY (duel_id)   REFERENCES duels(id)   ON DELETE CASCADE,
    CONSTRAINT uq_mr_player_duel
        UNIQUE (player_id, duel_id)        -- one result per player per duel
);

-- ── scores (cache) ────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS scores (
    player_id   INT         NOT NULL,
    final_score INT         NOT NULL DEFAULT 0,
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_scores PRIMARY KEY (player_id),
    CONSTRAINT fk_scores_player
        FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
    CONSTRAINT chk_score_non_negative
        CHECK (final_score >= 0)
);

-- ── Indexes ───────────────────────────────────────────────────

CREATE INDEX IF NOT EXISTS idx_players_tournament
    ON players(tournament_id);

CREATE INDEX IF NOT EXISTS idx_duels_tournament_order
    ON duels(tournament_id, duel_order);

CREATE INDEX IF NOT EXISTS idx_mr_player_order
    ON match_results(player_id, match_order);

-- ── Trigger: auto-refresh scores.updated_at ──────────────────

CREATE OR REPLACE FUNCTION trg_scores_updated_at()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS scores_updated_at ON scores;
CREATE TRIGGER scores_updated_at
    BEFORE UPDATE ON scores
    FOR EACH ROW EXECUTE FUNCTION trg_scores_updated_at();
