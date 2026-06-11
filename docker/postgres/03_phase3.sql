-- Phase 3 migration — Seasons, Battlepass, Objectives, Season Rewards
-- Run after 02_phase2.sql

-- ── New enum types ────────────────────────────────────────────────────────

DO $$ BEGIN
    CREATE TYPE season_status AS ENUM ('UPCOMING', 'ACTIVE', 'ENDED');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE objective_type AS ENUM (
        'WIN_DUELS', 'WIN_STREAK', 'SCORE_POINTS',
        'PLAY_TOURNAMENTS', 'EARN_BONUS',
        'PLAY_WITHOUT_PENALTY', 'REACH_SCORE_IN_MATCH'
    );
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE reset_type AS ENUM ('NONE', 'DAILY', 'WEEKLY');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE reward_type AS ENUM ('SKIN', 'TITLE', 'XP_BOOST', 'CURRENCY', 'NONE');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- ── seasons ───────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS seasons (
    id         SERIAL          NOT NULL,
    name       VARCHAR(100)    NOT NULL,
    status     season_status   NOT NULL DEFAULT 'UPCOMING',
    start_date TIMESTAMPTZ     NOT NULL,
    end_date   TIMESTAMPTZ     NOT NULL,
    created_at TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_seasons       PRIMARY KEY (id),
    CONSTRAINT uq_season_name   UNIQUE (name),
    CONSTRAINT chk_season_dates CHECK (start_date < end_date)
);

-- ── tournament_seasons ────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS tournament_seasons (
    tournament_id INT NOT NULL,
    season_id     INT NOT NULL,
    CONSTRAINT pk_tournament_seasons PRIMARY KEY (tournament_id),
    CONSTRAINT fk_ts_tournament      FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE,
    CONSTRAINT fk_ts_season          FOREIGN KEY (season_id)     REFERENCES seasons(id)     ON DELETE CASCADE
);

-- ── seasonal_player_stats ─────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS seasonal_player_stats (
    id                 SERIAL      NOT NULL,
    player_id          INT         NOT NULL,
    season_id          INT         NOT NULL,
    total_score        INT         NOT NULL DEFAULT 0,
    tournaments_played INT         NOT NULL DEFAULT 0,
    total_wins         INT         NOT NULL DEFAULT 0,
    total_draws        INT         NOT NULL DEFAULT 0,
    total_losses       INT         NOT NULL DEFAULT 0,
    win_streak_best    INT         NOT NULL DEFAULT 0,
    season_rank        INT,
    last_updated       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_seasonal_stats      PRIMARY KEY (id),
    CONSTRAINT uq_player_season       UNIQUE (player_id, season_id),
    CONSTRAINT fk_sps_player          FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
    CONSTRAINT fk_sps_season          FOREIGN KEY (season_id) REFERENCES seasons(id) ON DELETE CASCADE,
    CONSTRAINT chk_stats_non_negative CHECK (
        total_score >= 0 AND tournaments_played >= 0 AND
        total_wins  >= 0 AND total_draws >= 0 AND
        total_losses >= 0 AND win_streak_best >= 0
    )
);

-- ── battlepasses ──────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS battlepasses (
    id                SERIAL      NOT NULL,
    season_id         INT         NOT NULL,
    total_tiers       SMALLINT    NOT NULL DEFAULT 100,
    has_premium_track BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_battlepasses  PRIMARY KEY (id),
    CONSTRAINT uq_bp_season     UNIQUE (season_id),
    CONSTRAINT fk_bp_season     FOREIGN KEY (season_id) REFERENCES seasons(id) ON DELETE CASCADE,
    CONSTRAINT chk_total_tiers  CHECK (total_tiers > 0)
);

-- ── battlepass_tiers ──────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS battlepass_tiers (
    id            SERIAL      NOT NULL,
    battlepass_id INT         NOT NULL,
    tier_number   SMALLINT    NOT NULL,
    xp_required   INT         NOT NULL,
    is_premium    BOOLEAN     NOT NULL DEFAULT FALSE,
    reward_type   reward_type NOT NULL DEFAULT 'NONE',
    reward_data   JSONB       NOT NULL DEFAULT '{}',
    CONSTRAINT pk_bp_tiers       PRIMARY KEY (id),
    CONSTRAINT uq_tier_per_track UNIQUE (battlepass_id, tier_number, is_premium),
    CONSTRAINT fk_tier_bp        FOREIGN KEY (battlepass_id) REFERENCES battlepasses(id) ON DELETE CASCADE,
    CONSTRAINT chk_xp_positive   CHECK (xp_required > 0)
);

CREATE INDEX IF NOT EXISTS idx_bp_tiers_lookup
    ON battlepass_tiers(battlepass_id, is_premium, xp_required);

-- ── player_battlepass_progress ────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS player_battlepass_progress (
    id                  SERIAL      NOT NULL,
    player_id           INT         NOT NULL,
    battlepass_id       INT         NOT NULL,
    current_xp          INT         NOT NULL DEFAULT 0,
    current_tier        SMALLINT    NOT NULL DEFAULT 0,
    is_premium_unlocked BOOLEAN     NOT NULL DEFAULT FALSE,
    last_updated        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_bp_progress        PRIMARY KEY (id),
    CONSTRAINT uq_player_bp          UNIQUE (player_id, battlepass_id),
    CONSTRAINT fk_prog_player        FOREIGN KEY (player_id)     REFERENCES players(id)      ON DELETE CASCADE,
    CONSTRAINT fk_prog_bp            FOREIGN KEY (battlepass_id) REFERENCES battlepasses(id) ON DELETE CASCADE,
    CONSTRAINT chk_xp_non_negative   CHECK (current_xp >= 0),
    CONSTRAINT chk_tier_non_negative CHECK (current_tier >= 0)
);

-- ── objectives ────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS objectives (
    id             SERIAL         NOT NULL,
    season_id      INT            NOT NULL,
    name           VARCHAR(150)   NOT NULL,
    description    TEXT           NOT NULL,
    objective_type objective_type NOT NULL,
    target_value   INT            NOT NULL,
    xp_reward      INT            NOT NULL,
    reset_type     reset_type     NOT NULL DEFAULT 'NONE',
    is_active      BOOLEAN        NOT NULL DEFAULT TRUE,
    created_at     TIMESTAMPTZ    NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_objectives       PRIMARY KEY (id),
    CONSTRAINT fk_obj_season       FOREIGN KEY (season_id) REFERENCES seasons(id) ON DELETE CASCADE,
    CONSTRAINT chk_target_positive CHECK (target_value > 0),
    CONSTRAINT chk_xp_positive     CHECK (xp_reward > 0)
);

CREATE INDEX IF NOT EXISTS idx_objectives_season_active
    ON objectives(season_id, is_active, reset_type);

-- ── player_objective_completions ──────────────────────────────────────────
-- period_key: NULL for NONE (seasonal), 'YYYY-MM-DD' for DAILY, 'YYYY-WNN' for WEEKLY

CREATE TABLE IF NOT EXISTS player_objective_completions (
    id           BIGSERIAL   NOT NULL,
    player_id    INT         NOT NULL,
    objective_id INT         NOT NULL,
    completed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    xp_awarded   INT         NOT NULL,
    period_key   VARCHAR(20),
    CONSTRAINT pk_obj_completions  PRIMARY KEY (id),
    CONSTRAINT fk_oc_player        FOREIGN KEY (player_id)    REFERENCES players(id)    ON DELETE CASCADE,
    CONSTRAINT fk_oc_objective     FOREIGN KEY (objective_id) REFERENCES objectives(id) ON DELETE CASCADE
);

-- Seasonal objectives (reset_type = NONE) can only be completed once
CREATE UNIQUE INDEX IF NOT EXISTS uq_seasonal_objective_completion
    ON player_objective_completions(player_id, objective_id)
    WHERE period_key IS NULL;

CREATE INDEX IF NOT EXISTS idx_oc_player_objective
    ON player_objective_completions(player_id, objective_id);

-- ── season_rewards ────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS season_rewards (
    id          SERIAL       NOT NULL,
    season_id   INT          NOT NULL,
    rank_min    INT          NOT NULL,
    rank_max    INT,                   -- NULL = all ranks >= rank_min
    reward_type reward_type  NOT NULL,
    reward_data JSONB        NOT NULL DEFAULT '{}',
    label       VARCHAR(100) NOT NULL,
    CONSTRAINT pk_season_rewards PRIMARY KEY (id),
    CONSTRAINT fk_sr_season      FOREIGN KEY (season_id) REFERENCES seasons(id) ON DELETE CASCADE,
    CONSTRAINT chk_rank_min      CHECK (rank_min > 0),
    CONSTRAINT chk_rank_range    CHECK (rank_max IS NULL OR rank_max >= rank_min)
);

-- ── player_season_rewards ─────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS player_season_rewards (
    id               SERIAL      NOT NULL,
    player_id        INT         NOT NULL,
    season_id        INT         NOT NULL,
    season_rank      INT         NOT NULL,
    season_reward_id INT         NOT NULL,
    awarded_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_player_season_rewards  PRIMARY KEY (id),
    CONSTRAINT uq_player_season_reward   UNIQUE (player_id, season_id, season_reward_id),
    CONSTRAINT fk_psr_player             FOREIGN KEY (player_id)        REFERENCES players(id)        ON DELETE CASCADE,
    CONSTRAINT fk_psr_season             FOREIGN KEY (season_id)        REFERENCES seasons(id)        ON DELETE CASCADE,
    CONSTRAINT fk_psr_reward             FOREIGN KEY (season_reward_id) REFERENCES season_rewards(id)
);

-- ── Seed data ─────────────────────────────────────────────────────────────

INSERT INTO seasons (name, status, start_date, end_date) VALUES
    ('Saison 1 — L''Aube des Chevaliers', 'UPCOMING',
     '2026-07-01 00:00:00+00', '2026-09-30 23:59:59+00')
ON CONFLICT DO NOTHING;

INSERT INTO battlepasses (season_id, total_tiers, has_premium_track) VALUES (1, 100, TRUE)
ON CONFLICT DO NOTHING;

INSERT INTO battlepass_tiers (battlepass_id, tier_number, xp_required, is_premium, reward_type, reward_data) VALUES
    (1, 10,  1000,  FALSE, 'TITLE',    '{"title": "Écuyer", "color": "#C0C0C0"}'),
    (1, 25,  2500,  FALSE, 'SKIN',     '{"skin_id": 1, "skin_name": "Chevalier classique"}'),
    (1, 50,  5000,  FALSE, 'CURRENCY', '{"amount": 500, "currency": "GOLD"}'),
    (1, 75,  7500,  FALSE, 'TITLE',    '{"title": "Chevalier", "color": "#FFD700"}'),
    (1, 100, 10000, FALSE, 'SKIN',     '{"skin_id": 2, "skin_name": "Paladin doré"}'),
    (1, 10,  1000,  TRUE,  'CURRENCY', '{"amount": 200, "currency": "GOLD"}'),
    (1, 50,  5000,  TRUE,  'SKIN',     '{"skin_id": 3, "skin_name": "Chevalier de Minuit"}'),
    (1, 100, 10000, TRUE,  'SKIN',     '{"skin_id": 4, "skin_name": "Paladin de Cristal"}')
ON CONFLICT DO NOTHING;

INSERT INTO objectives (season_id, name, description, objective_type, target_value, xp_reward, reset_type) VALUES
    (1, 'Premier sang',          'Remporter votre premier duel',              'WIN_DUELS',            1,   100, 'NONE'),
    (1, 'Enchaîner les duels',   'Gagner 3 duels d''affilée',                 'WIN_STREAK',           3,   500, 'NONE'),
    (1, 'Vétéran',               'Participer à 5 tournois',                   'PLAY_TOURNAMENTS',     5,   800, 'NONE'),
    (1, 'Sans reproche',         'Terminer un tournoi sans pénalité',         'PLAY_WITHOUT_PENALTY', 1,   300, 'NONE'),
    (1, 'Champion saisonnier',   'Accumuler 100 points sur la saison',        'SCORE_POINTS',         100, 1500, 'NONE'),
    (1, 'Défi du jour — victoire','Remporter 1 duel aujourd''hui',            'WIN_DUELS',            1,   50,  'DAILY'),
    (1, 'Tournoi du jour',       'Participer à 1 tournoi aujourd''hui',       'PLAY_TOURNAMENTS',     1,   75,  'DAILY'),
    (1, 'Semaine glorieuse',     'Remporter 5 duels cette semaine',           'WIN_DUELS',            5,   300, 'WEEKLY'),
    (1, 'Série magique',         'Déclencher le bonus de série 2 fois',       'EARN_BONUS',           2,   400, 'WEEKLY')
ON CONFLICT DO NOTHING;

INSERT INTO season_rewards (season_id, rank_min, rank_max, reward_type, reward_data, label) VALUES
    (1, 1,   1,    'SKIN',  '{"skin_id": 5, "skin_name": "Armure du Champion"}',          'Champion de la Saison'),
    (1, 2,   10,   'SKIN',  '{"skin_id": 6, "skin_name": "Armure de l''Élite"}',          'Top 10'),
    (1, 11,  50,   'TITLE', '{"title": "Chevalier de la Saison 1", "color": "#4169E1"}',  'Top 50'),
    (1, 51,  NULL, 'TITLE', '{"title": "Participant Saison 1", "color": "#808080"}',       'Participant')
ON CONFLICT DO NOTHING;
