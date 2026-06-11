-- Phase 2 migration — Replays, Skins, Cosmetic Snapshots
-- Run after 01_init.sql

-- ── New enum types ────────────────────────────────────────────────────────

DO $$ BEGIN
    CREATE TYPE event_type AS ENUM (
        'DUEL_START', 'ATTACK', 'PARRY', 'TOUCH',
        'PENALTY', 'TIMEOUT', 'DISQUALIFY', 'DUEL_END'
    );
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE skin_category AS ENUM ('PLAYER', 'BACKGROUND');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- ── Replay ────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS duel_replays (
    id             SERIAL      NOT NULL,
    duel_id        INT         NOT NULL,
    schema_version SMALLINT    NOT NULL DEFAULT 1,
    recorded_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_complete    BOOLEAN     NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_duel_replays  PRIMARY KEY (id),
    CONSTRAINT uq_replay_duel   UNIQUE (duel_id),
    CONSTRAINT fk_replay_duel   FOREIGN KEY (duel_id) REFERENCES duels(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS duel_replay_events (
    id               BIGSERIAL  NOT NULL,
    replay_id        INT        NOT NULL,
    event_order      INT        NOT NULL,
    event_type       event_type NOT NULL,
    actor_player_id  INT,
    target_player_id INT,
    occurred_at_ms   INT        NOT NULL,
    payload          JSONB,
    CONSTRAINT pk_replay_events PRIMARY KEY (id),
    CONSTRAINT uq_re_order      UNIQUE (replay_id, event_order),
    CONSTRAINT fk_re_replay     FOREIGN KEY (replay_id)        REFERENCES duel_replays(id) ON DELETE CASCADE,
    CONSTRAINT fk_re_actor      FOREIGN KEY (actor_player_id)  REFERENCES players(id),
    CONSTRAINT fk_re_target     FOREIGN KEY (target_player_id) REFERENCES players(id)
);

CREATE INDEX IF NOT EXISTS idx_replay_events_order
    ON duel_replay_events(replay_id, event_order);

-- ── Skins catalog ─────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS skins (
    id         SERIAL        NOT NULL,
    category   skin_category NOT NULL,
    name       VARCHAR(100)  NOT NULL,
    asset_key  VARCHAR(200)  NOT NULL,
    is_premium BOOLEAN       NOT NULL DEFAULT FALSE,
    is_active  BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_skins      PRIMARY KEY (id),
    CONSTRAINT uq_skin_name  UNIQUE (name),
    CONSTRAINT uq_skin_asset UNIQUE (asset_key)
);

-- ── Player skin loadout ───────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS player_skin_loadouts (
    id          SERIAL      NOT NULL,
    player_id   INT         NOT NULL,
    skin_id     INT         NOT NULL,
    equipped_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_loadout        PRIMARY KEY (id),
    CONSTRAINT fk_loadout_player FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
    CONSTRAINT fk_loadout_skin   FOREIGN KEY (skin_id)   REFERENCES skins(id)
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_one_player_skin
    ON player_skin_loadouts(player_id);

-- ── Tournament background loadout ─────────────────────────────────────────

CREATE TABLE IF NOT EXISTS tournament_background_loadouts (
    tournament_id INT         NOT NULL,
    skin_id       INT         NOT NULL,
    set_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_bg_loadout    PRIMARY KEY (tournament_id),
    CONSTRAINT fk_bg_tournament FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE,
    CONSTRAINT fk_bg_skin       FOREIGN KEY (skin_id)       REFERENCES skins(id)
);

-- ── Cosmetic snapshot (replay isolation) ──────────────────────────────────

CREATE TABLE IF NOT EXISTS duel_cosmetic_snapshots (
    duel_id              INT          NOT NULL,
    player1_skin_name    VARCHAR(100),
    player1_asset_key    VARCHAR(200),
    player2_skin_name    VARCHAR(100),
    player2_asset_key    VARCHAR(200),
    background_skin_name VARCHAR(100),
    background_asset_key VARCHAR(200),
    snapshotted_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_cosmetic_snapshot PRIMARY KEY (duel_id),
    CONSTRAINT fk_cs_duel           FOREIGN KEY (duel_id) REFERENCES duels(id) ON DELETE CASCADE
);

-- ── Seed data ─────────────────────────────────────────────────────────────

INSERT INTO skins (category, name, asset_key, is_premium) VALUES
    ('PLAYER',     'Classic Knight',  'skins/player/classic_knight',  FALSE),
    ('PLAYER',     'Golden Paladin',  'skins/player/golden_paladin',  TRUE),
    ('BACKGROUND', 'Stone Arena',     'skins/bg/stone_arena',         FALSE),
    ('BACKGROUND', 'Ghost Castle',    'skins/bg/ghost_castle',        TRUE)
ON CONFLICT DO NOTHING;
