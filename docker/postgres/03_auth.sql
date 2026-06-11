-- ============================================================
-- Phase 3 – Authentication
-- ============================================================

CREATE TABLE users (
    id                       SERIAL PRIMARY KEY,
    email                    VARCHAR(255) NOT NULL UNIQUE,
    password_hash            VARCHAR(255) NOT NULL,
    player_id                INT REFERENCES players(id) ON DELETE SET NULL,
    refresh_token            VARCHAR(512),
    refresh_token_expires_at TIMESTAMPTZ,
    created_at               TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_users_email         ON users (email);
CREATE INDEX idx_users_refresh_token ON users (refresh_token) WHERE refresh_token IS NOT NULL;
