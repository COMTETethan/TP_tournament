# Schéma BDD Phase 3 — Saisons, Battlepass, Objectifs et Récompenses saisonnières

---

## 1. Principes de conception

### Règle cardinale : la saison n'altère pas les données de jeu

Les saisons et le battlepass sont une **couche de progression orthogonale** au système de tournoi. Elles ne modifient aucune table de Phase 1 ou Phase 2 :
- Un tournoi peut appartenir à 0 ou 1 saison (`tournament_seasons`, table de liaison optionnelle)
- Les `match_results` et `scores` restent inchangés — les stats saisonnières sont un **agrégat** calculé, pas une duplication

### Séparation récompenses / cosmétiques

Les récompenses peuvent débloquer des skins (Phase 2). Le lien se fait via `reward_data JSONB` qui peut référencer `skins.id` — mais une récompense n'est pas un skin, c'est un événement d'attribution. Quand une récompense octroie un skin, elle insère dans `player_skin_inventory` (table définie en Phase 2 comme future extension).

### Objectifs : déclenchés par les événements de jeu, jamais inversement

Les objectifs lisent les données de jeu (résultats de duels, scores). Ils n'affectent jamais les règles de calcul de `ScoreCalculator`. Un joueur ne peut pas modifier son score de tournoi en complétant des objectifs — seul le battlepass XP est affecté.

---

## 2. Nouvelles tables

### `seasons`
Une saison = une période de temps avec un nom et un état.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | SERIAL | PK | Identifiant unique |
| `name` | VARCHAR(100) | NOT NULL, UNIQUE | Ex. : "Saison 1 — L'Aube des Chevaliers" |
| `status` | season_status (ENUM) | NOT NULL, DEFAULT 'UPCOMING' | UPCOMING → ACTIVE → ENDED |
| `start_date` | TIMESTAMPTZ | NOT NULL | Début de la saison |
| `end_date` | TIMESTAMPTZ | NOT NULL | Fin de la saison |
| `created_at` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |

**Enum `season_status` :** `UPCOMING`, `ACTIVE`, `ENDED`

**Contrainte :** `start_date < end_date`

---

### `tournament_seasons`
Rattache un tournoi existant à une saison. Un tournoi peut ne pas appartenir à une saison (hors-saison).

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `tournament_id` | INT | PK, FK → tournaments.id | Tournoi |
| `season_id` | INT | FK → seasons.id, NOT NULL | Saison |

**Contrainte :** un tournoi appartient à au plus une saison (PKsur `tournament_id`).

---

### `seasonal_player_stats`
Stats agrégées d'un joueur pour une saison donnée, mises à jour après chaque tournoi.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | SERIAL | PK | |
| `player_id` | INT | FK → players.id, NOT NULL | |
| `season_id` | INT | FK → seasons.id, NOT NULL | |
| `total_score` | INT | NOT NULL, DEFAULT 0, CHECK >= 0 | Somme des scores finaux sur tous les tournois de la saison |
| `tournaments_played` | INT | NOT NULL, DEFAULT 0 | Nombre de tournois disputés |
| `total_wins` | INT | NOT NULL, DEFAULT 0 | Nombre total de victoires en duel |
| `total_draws` | INT | NOT NULL, DEFAULT 0 | |
| `total_losses` | INT | NOT NULL, DEFAULT 0 | |
| `win_streak_best` | INT | NOT NULL, DEFAULT 0 | Meilleure série de victoires consécutives |
| `season_rank` | INT | NULLABLE | Rang final (calculé à la clôture de la saison) |
| `last_updated` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |

**Contrainte UNIQUE :** `(player_id, season_id)`

---

### `battlepasses`
Un battlepass par saison. Contient les métadonnées de progression.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | SERIAL | PK | |
| `season_id` | INT | FK → seasons.id, NOT NULL, UNIQUE | Un seul battlepass par saison |
| `total_tiers` | SMALLINT | NOT NULL, DEFAULT 100 | Nombre total de paliers (ex. 100) |
| `has_premium_track` | BOOLEAN | NOT NULL, DEFAULT TRUE | Active la piste premium |
| `created_at` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |

---

### `battlepass_tiers`
Définition de chaque palier : XP cumulatif requis et récompense.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | SERIAL | PK | |
| `battlepass_id` | INT | FK → battlepasses.id, NOT NULL | |
| `tier_number` | SMALLINT | NOT NULL | 1 à `total_tiers` |
| `xp_required` | INT | NOT NULL, CHECK > 0 | XP **cumulatif** depuis 0 pour débloquer ce palier |
| `is_premium` | BOOLEAN | NOT NULL, DEFAULT FALSE | Palier sur la piste premium (nécessite déblocage) |
| `reward_type` | reward_type (ENUM) | NOT NULL | Type de récompense |
| `reward_data` | JSONB | NOT NULL | Données spécifiques à la récompense |

**Enum `reward_type` :** `SKIN`, `TITLE`, `XP_BOOST`, `CURRENCY`, `NONE`

**Contrainte UNIQUE :** `(battlepass_id, tier_number, is_premium)`

**Exemples de `reward_data` :**
```jsonc
// SKIN
{ "skin_id": 12, "skin_name": "Paladin de Cristal" }

// TITLE
{ "title": "Chevalier de la Table Ronde", "color": "#FFD700" }

// XP_BOOST
{ "multiplier": 2.0, "duration_hours": 24 }

// CURRENCY
{ "amount": 500, "currency": "GOLD" }
```

---

### `player_battlepass_progress`
Progression XP et palier actuel d'un joueur pour un battlepass.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | SERIAL | PK | |
| `player_id` | INT | FK → players.id, NOT NULL | |
| `battlepass_id` | INT | FK → battlepasses.id, NOT NULL | |
| `current_xp` | INT | NOT NULL, DEFAULT 0, CHECK >= 0 | XP total accumulé |
| `current_tier` | SMALLINT | NOT NULL, DEFAULT 0 | Palier atteint (0 = aucun palier débloqué) |
| `is_premium_unlocked` | BOOLEAN | NOT NULL, DEFAULT FALSE | A acheté la piste premium |
| `last_updated` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |

**Contrainte UNIQUE :** `(player_id, battlepass_id)`

---

### `objectives`
Catalogue des objectifs disponibles dans une saison.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | SERIAL | PK | |
| `season_id` | INT | FK → seasons.id, NOT NULL | Saison d'appartenance |
| `name` | VARCHAR(150) | NOT NULL | Ex. : "Remporter 3 duels" |
| `description` | TEXT | NOT NULL | Description affichée |
| `objective_type` | objective_type (ENUM) | NOT NULL | Type de déclencheur |
| `target_value` | INT | NOT NULL, CHECK > 0 | Seuil à atteindre (ex. : 3 victoires) |
| `xp_reward` | INT | NOT NULL, CHECK > 0 | XP accordé à la complétion |
| `reset_type` | reset_type (ENUM) | NOT NULL, DEFAULT 'NONE' | Fréquence de réinitialisation |
| `is_active` | BOOLEAN | NOT NULL, DEFAULT TRUE | Soft-delete |
| `created_at` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |

**Enum `objective_type` :**
```sql
CREATE TYPE objective_type AS ENUM (
    'WIN_DUELS',             -- Remporter N duels
    'WIN_STREAK',            -- Gagner N duels d'affilée
    'SCORE_POINTS',          -- Accumuler N points sur la saison
    'PLAY_TOURNAMENTS',      -- Participer à N tournois
    'EARN_BONUS',            -- Déclencher le bonus de série N fois
    'PLAY_WITHOUT_PENALTY',  -- Terminer N tournois sans pénalité
    'REACH_SCORE_IN_MATCH'   -- Atteindre N points dans un seul tournoi
);
```

**Enum `reset_type` :** `NONE` (saisonnier, une fois), `DAILY`, `WEEKLY`

---

### `player_objective_completions`
Trace chaque complétion d'objectif par un joueur.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | BIGSERIAL | PK | |
| `player_id` | INT | FK → players.id, NOT NULL | |
| `objective_id` | INT | FK → objectives.id, NOT NULL | |
| `completed_at` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |
| `xp_awarded` | INT | NOT NULL | XP effectivement accordé (peut différer si boost actif) |
| `period_key` | VARCHAR(20) | NULLABLE | Pour les objectifs reset : "2026-06-10" (daily) ou "2026-W24" (weekly) |

**Contrainte UNIQUE pour les objectifs NONE (saisonniers) :** `(player_id, objective_id)` WHERE `period_key IS NULL`
> Les objectifs DAILY/WEEKLY peuvent être complétés plusieurs fois, identifiés par `period_key`.

---

### `season_rewards`
Catalogue des récompenses attribuées à la fin d'une saison selon le classement final.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | SERIAL | PK | |
| `season_id` | INT | FK → seasons.id, NOT NULL | |
| `rank_min` | INT | NOT NULL, CHECK > 0 | Rang minimum inclus (ex. : 1) |
| `rank_max` | INT | NULLABLE | Rang maximum inclus (NULL = tous les rangs >= rank_min) |
| `reward_type` | reward_type (ENUM) | NOT NULL | |
| `reward_data` | JSONB | NOT NULL | |
| `label` | VARCHAR(100) | NOT NULL | Ex. : "Champion de la Saison", "Top 10%" |

**Exemples de plages de classement :**
```
rank_min=1,  rank_max=1    → Champion (skin légendaire + titre)
rank_min=2,  rank_max=10   → Top 10 (skin épique)
rank_min=11, rank_max=NULL → Participants (titre de participation)
```

---

### `player_season_rewards`
Récompenses effectivement distribuées après la clôture de la saison.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | SERIAL | PK | |
| `player_id` | INT | FK → players.id, NOT NULL | |
| `season_id` | INT | FK → seasons.id, NOT NULL | |
| `season_rank` | INT | NOT NULL | Rang final du joueur dans la saison |
| `season_reward_id` | INT | FK → season_rewards.id, NOT NULL | Récompense attribuée |
| `awarded_at` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | |

**Contrainte UNIQUE :** `(player_id, season_id, season_reward_id)`

---

## 3. Diagramme Entité-Relation — Phase 3 complète

```
                    ┌─────────────────────────────────────────────────────────────┐
                    │                       PHASE 1                               │
                    │  tournaments ──── players ──── duels ──── match_results     │
                    │                     │                                        │
                    └─────────────────────┼────────────────────────────────────────┘
                                          │
                          ┌───────────────┼──────────────────────────────────────────┐
                          │               │              PHASE 3                      │
                          │               │                                           │
    tournaments ──────────┤               │                                           │
         │                │               ▼                                           │
         │ N              │        seasonal_player_stats                              │
         ▼ 1              │        ─────────────────────                              │
    tournament_seasons    │        player_id  FK→players                             │
         │ N              │        season_id  FK→seasons                             │
         ▼ 1              │        total_score, wins, streak_best, season_rank       │
       seasons ───────────┤                                                           │
         │                │                                                           │
         │ 1              │                                                           │
         ├──────────────► objectives (type, target, xp_reward, reset_type)          │
         │                │      │ N                                                 │
         │ 1              │      ▼ N                                                 │
         ├──────────────► player_objective_completions                               │
         │                │      (player_id, period_key, xp_awarded)                │
         │ 1              │                                                           │
         ├──────────────► battlepasses ──────► battlepass_tiers                     │
         │                │      │ 1                 (tier_number, xp_required,     │
         │                │      │                    is_premium, reward_type,       │
         │ 1              │      │                    reward_data JSONB)             │
         ├──────────────► season_rewards       │                                     │
         │                │      │ N           ▼ N                                   │
         │                │      ▼          player_battlepass_progress               │
         │                │  player_season_rewards  (current_xp, current_tier,      │
         │                │     (season_rank,         is_premium_unlocked)           │
         │                │      awarded_at)                                         │
         └────────────────┘                                                          │
                                                                                     │
                    ┌────────────────────────────────────────────────────────────────┘
                    │                       PHASE 2
                    │  skins ◄── reward_data (JSONB ref) ── battlepass_tiers
                    │  skins ◄── reward_data (JSONB ref) ── season_rewards
                    └─────────────────────────────────────────────────────────────────
```

---

## 4. Scripts SQL — Phase 3

```sql
-- ── New enum types ─────────────────────────────────────────────

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

-- ── seasons ────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS seasons (
    id         SERIAL          NOT NULL,
    name       VARCHAR(100)    NOT NULL,
    status     season_status   NOT NULL DEFAULT 'UPCOMING',
    start_date TIMESTAMPTZ     NOT NULL,
    end_date   TIMESTAMPTZ     NOT NULL,
    created_at TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_seasons         PRIMARY KEY (id),
    CONSTRAINT uq_season_name     UNIQUE (name),
    CONSTRAINT chk_season_dates   CHECK (start_date < end_date)
);

-- ── tournament_seasons ─────────────────────────────────────────

CREATE TABLE IF NOT EXISTS tournament_seasons (
    tournament_id INT NOT NULL,
    season_id     INT NOT NULL,
    CONSTRAINT pk_tournament_seasons    PRIMARY KEY (tournament_id),
    CONSTRAINT fk_ts_tournament         FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE,
    CONSTRAINT fk_ts_season             FOREIGN KEY (season_id)     REFERENCES seasons(id)     ON DELETE CASCADE
);

-- ── seasonal_player_stats ──────────────────────────────────────

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
    CONSTRAINT pk_seasonal_stats          PRIMARY KEY (id),
    CONSTRAINT uq_player_season           UNIQUE (player_id, season_id),
    CONSTRAINT fk_sps_player              FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
    CONSTRAINT fk_sps_season              FOREIGN KEY (season_id) REFERENCES seasons(id)  ON DELETE CASCADE,
    CONSTRAINT chk_stats_non_negative     CHECK (
        total_score >= 0 AND tournaments_played >= 0 AND
        total_wins >= 0  AND total_draws >= 0 AND
        total_losses >= 0 AND win_streak_best >= 0
    )
);

-- ── battlepasses ───────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS battlepasses (
    id                   SERIAL      NOT NULL,
    season_id            INT         NOT NULL,
    total_tiers          SMALLINT    NOT NULL DEFAULT 100,
    has_premium_track    BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_battlepasses      PRIMARY KEY (id),
    CONSTRAINT uq_bp_season         UNIQUE (season_id),
    CONSTRAINT fk_bp_season         FOREIGN KEY (season_id) REFERENCES seasons(id) ON DELETE CASCADE,
    CONSTRAINT chk_total_tiers      CHECK (total_tiers > 0)
);

-- ── battlepass_tiers ───────────────────────────────────────────

CREATE TABLE IF NOT EXISTS battlepass_tiers (
    id             SERIAL      NOT NULL,
    battlepass_id  INT         NOT NULL,
    tier_number    SMALLINT    NOT NULL,
    xp_required    INT         NOT NULL,
    is_premium     BOOLEAN     NOT NULL DEFAULT FALSE,
    reward_type    reward_type NOT NULL DEFAULT 'NONE',
    reward_data    JSONB       NOT NULL DEFAULT '{}',
    CONSTRAINT pk_bp_tiers        PRIMARY KEY (id),
    CONSTRAINT uq_tier_per_track  UNIQUE (battlepass_id, tier_number, is_premium),
    CONSTRAINT fk_tier_bp         FOREIGN KEY (battlepass_id) REFERENCES battlepasses(id) ON DELETE CASCADE,
    CONSTRAINT chk_xp_positive    CHECK (xp_required > 0)
);

CREATE INDEX IF NOT EXISTS idx_bp_tiers_lookup
    ON battlepass_tiers(battlepass_id, is_premium, xp_required);

-- ── player_battlepass_progress ─────────────────────────────────

CREATE TABLE IF NOT EXISTS player_battlepass_progress (
    id                   SERIAL      NOT NULL,
    player_id            INT         NOT NULL,
    battlepass_id        INT         NOT NULL,
    current_xp           INT         NOT NULL DEFAULT 0,
    current_tier         SMALLINT    NOT NULL DEFAULT 0,
    is_premium_unlocked  BOOLEAN     NOT NULL DEFAULT FALSE,
    last_updated         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_bp_progress       PRIMARY KEY (id),
    CONSTRAINT uq_player_bp         UNIQUE (player_id, battlepass_id),
    CONSTRAINT fk_prog_player       FOREIGN KEY (player_id)      REFERENCES players(id)     ON DELETE CASCADE,
    CONSTRAINT fk_prog_bp           FOREIGN KEY (battlepass_id)  REFERENCES battlepasses(id) ON DELETE CASCADE,
    CONSTRAINT chk_xp_non_negative  CHECK (current_xp >= 0),
    CONSTRAINT chk_tier_non_negative CHECK (current_tier >= 0)
);

-- ── objectives ─────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS objectives (
    id              SERIAL          NOT NULL,
    season_id       INT             NOT NULL,
    name            VARCHAR(150)    NOT NULL,
    description     TEXT            NOT NULL,
    objective_type  objective_type  NOT NULL,
    target_value    INT             NOT NULL,
    xp_reward       INT             NOT NULL,
    reset_type      reset_type      NOT NULL DEFAULT 'NONE',
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_objectives        PRIMARY KEY (id),
    CONSTRAINT fk_obj_season        FOREIGN KEY (season_id) REFERENCES seasons(id) ON DELETE CASCADE,
    CONSTRAINT chk_target_positive  CHECK (target_value > 0),
    CONSTRAINT chk_xp_positive      CHECK (xp_reward > 0)
);

CREATE INDEX IF NOT EXISTS idx_objectives_season_active
    ON objectives(season_id, is_active, reset_type);

-- ── player_objective_completions ───────────────────────────────

CREATE TABLE IF NOT EXISTS player_objective_completions (
    id           BIGSERIAL   NOT NULL,
    player_id    INT         NOT NULL,
    objective_id INT         NOT NULL,
    completed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    xp_awarded   INT         NOT NULL,
    period_key   VARCHAR(20),        -- NULL for NONE, "2026-06-10" for DAILY, "2026-W24" for WEEKLY
    CONSTRAINT pk_obj_completions       PRIMARY KEY (id),
    CONSTRAINT fk_oc_player             FOREIGN KEY (player_id)    REFERENCES players(id)    ON DELETE CASCADE,
    CONSTRAINT fk_oc_objective          FOREIGN KEY (objective_id) REFERENCES objectives(id) ON DELETE CASCADE
);

-- Prevent double-completion of seasonal objectives (reset_type = NONE)
CREATE UNIQUE INDEX IF NOT EXISTS uq_seasonal_objective_completion
    ON player_objective_completions(player_id, objective_id)
    WHERE period_key IS NULL;

CREATE INDEX IF NOT EXISTS idx_oc_player_season
    ON player_objective_completions(player_id, objective_id);

-- ── season_rewards ─────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS season_rewards (
    id          SERIAL      NOT NULL,
    season_id   INT         NOT NULL,
    rank_min    INT         NOT NULL,
    rank_max    INT,                  -- NULL = tous les rangs >= rank_min
    reward_type reward_type NOT NULL,
    reward_data JSONB       NOT NULL DEFAULT '{}',
    label       VARCHAR(100) NOT NULL,
    CONSTRAINT pk_season_rewards  PRIMARY KEY (id),
    CONSTRAINT fk_sr_season       FOREIGN KEY (season_id) REFERENCES seasons(id) ON DELETE CASCADE,
    CONSTRAINT chk_rank_min       CHECK (rank_min > 0),
    CONSTRAINT chk_rank_range     CHECK (rank_max IS NULL OR rank_max >= rank_min)
);

-- ── player_season_rewards ──────────────────────────────────────

CREATE TABLE IF NOT EXISTS player_season_rewards (
    id               SERIAL      NOT NULL,
    player_id        INT         NOT NULL,
    season_id        INT         NOT NULL,
    season_rank      INT         NOT NULL,
    season_reward_id INT         NOT NULL,
    awarded_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_player_season_rewards   PRIMARY KEY (id),
    CONSTRAINT uq_player_season_reward    UNIQUE (player_id, season_id, season_reward_id),
    CONSTRAINT fk_psr_player              FOREIGN KEY (player_id)        REFERENCES players(id)        ON DELETE CASCADE,
    CONSTRAINT fk_psr_season              FOREIGN KEY (season_id)        REFERENCES seasons(id)        ON DELETE CASCADE,
    CONSTRAINT fk_psr_reward              FOREIGN KEY (season_reward_id) REFERENCES season_rewards(id)
);
```

---

## 5. Requêtes clés

### Classement saisonnier (top 10)
```sql
SELECT
    p.name,
    sps.total_score,
    sps.tournaments_played,
    sps.total_wins,
    sps.win_streak_best,
    RANK() OVER (PARTITION BY sps.season_id ORDER BY sps.total_score DESC) AS rank
FROM   seasonal_player_stats sps
JOIN   players p ON p.id = sps.player_id
WHERE  sps.season_id = :seasonId
ORDER  BY rank
LIMIT  10;
```

### Progression battlepass d'un joueur
```sql
SELECT
    pbp.current_xp,
    pbp.current_tier,
    pbp.is_premium_unlocked,
    -- Prochain palier à débloquer
    next_tier.tier_number   AS next_tier_number,
    next_tier.xp_required   AS next_tier_xp_required,
    next_tier.xp_required - pbp.current_xp AS xp_remaining,
    next_tier.reward_type,
    next_tier.reward_data
FROM   player_battlepass_progress pbp
JOIN   battlepasses bp ON bp.id = pbp.battlepass_id
LEFT   JOIN battlepass_tiers next_tier
       ON  next_tier.battlepass_id = bp.id
       AND next_tier.tier_number   = pbp.current_tier + 1
       AND next_tier.is_premium    = FALSE  -- piste gratuite
WHERE  pbp.player_id     = :playerId
  AND  bp.season_id      = :seasonId;
```

### Objectifs actifs du jour pour un joueur (avec progression)
```sql
SELECT
    o.name,
    o.description,
    o.objective_type,
    o.target_value,
    o.xp_reward,
    o.reset_type,
    CASE
        WHEN o.reset_type = 'NONE'   THEN poc.id IS NOT NULL
        WHEN o.reset_type = 'DAILY'  THEN poc.period_key = TO_CHAR(NOW(), 'YYYY-MM-DD')
        WHEN o.reset_type = 'WEEKLY' THEN poc.period_key = TO_CHAR(NOW(), 'IYYY-"W"IW')
    END AS is_completed
FROM   objectives o
LEFT   JOIN player_objective_completions poc
       ON  poc.objective_id = o.id
       AND poc.player_id    = :playerId
WHERE  o.season_id  = :seasonId
  AND  o.is_active  = TRUE
ORDER  BY o.reset_type, o.name;
```

### Distribuer les récompenses de fin de saison
```sql
-- Étape 1 : calculer les rangs finaux
UPDATE seasonal_player_stats sps
SET    season_rank = ranked.final_rank
FROM (
    SELECT id,
           RANK() OVER (PARTITION BY season_id ORDER BY total_score DESC) AS final_rank
    FROM   seasonal_player_stats
    WHERE  season_id = :seasonId
) ranked
WHERE sps.id = ranked.id;

-- Étape 2 : distribuer les récompenses selon le rang
INSERT INTO player_season_rewards (player_id, season_id, season_rank, season_reward_id)
SELECT
    sps.player_id,
    sps.season_id,
    sps.season_rank,
    sr.id
FROM   seasonal_player_stats sps
JOIN   season_rewards sr
       ON  sr.season_id = sps.season_id
       AND sps.season_rank >= sr.rank_min
       AND (sr.rank_max IS NULL OR sps.season_rank <= sr.rank_max)
WHERE  sps.season_id = :seasonId
ON CONFLICT (player_id, season_id, season_reward_id) DO NOTHING;  -- idempotent

-- Étape 3 : marquer la saison comme terminée
UPDATE seasons SET status = 'ENDED' WHERE id = :seasonId;
```

---

## 6. Flux complet d'une saison

```
1. Création de la saison        → INSERT INTO seasons (status='UPCOMING')
2. Création du battlepass       → INSERT INTO battlepasses + battlepass_tiers
3. Création des objectifs       → INSERT INTO objectives (daily/weekly/saisonniers)
4. Rattachement des tournois    → INSERT INTO tournament_seasons
5. Démarrage de la saison       → UPDATE seasons SET status='ACTIVE'

── Pendant la saison ──
6. Fin d'un tournoi             → UPDATE seasonal_player_stats (total_score, wins...)
                                  Vérifier les objectifs complétés
7. Objectif complété            → INSERT INTO player_objective_completions
                                  UPDATE player_battlepass_progress SET current_xp += xp_awarded
8. Palier battlepass débloqué   → Vérifier next tier XP threshold
                                  Distribuer la récompense du palier (ex: INSERT INTO player_skin_inventory)
9. Reset daily/weekly           → Les objectifs reset_type='DAILY'/'WEEKLY' redeviennent disponibles
                                  (contrôlé via period_key, pas de DELETE)

── Fin de saison ──
10. Calcul des rangs finaux     → UPDATE seasonal_player_stats SET season_rank = RANK()
11. Distribution récompenses    → INSERT INTO player_season_rewards
12. Clôture                     → UPDATE seasons SET status='ENDED'
```

---

## 7. Données de seed — Saison 1

```sql
-- Saison
INSERT INTO seasons (name, status, start_date, end_date)
VALUES ('Saison 1 — L''Aube des Chevaliers', 'UPCOMING',
        '2026-07-01 00:00:00+00', '2026-09-30 23:59:59+00');

-- Battlepass (100 paliers)
INSERT INTO battlepasses (season_id, total_tiers, has_premium_track)
VALUES (1, 100, TRUE);

-- Paliers gratuits (exemples)
INSERT INTO battlepass_tiers (battlepass_id, tier_number, xp_required, is_premium, reward_type, reward_data)
VALUES
    (1, 10, 1000,  FALSE, 'TITLE',    '{"title": "Écuyer", "color": "#C0C0C0"}'),
    (1, 25, 2500,  FALSE, 'SKIN',     '{"skin_id": 1, "skin_name": "Chevalier classique"}'),
    (1, 50, 5000,  FALSE, 'CURRENCY', '{"amount": 500, "currency": "GOLD"}'),
    (1, 75, 7500,  FALSE, 'TITLE',    '{"title": "Chevalier", "color": "#FFD700"}'),
    (1, 100, 10000, FALSE, 'SKIN',    '{"skin_id": 2, "skin_name": "Paladin doré"}');

-- Paliers premium (exemples)
INSERT INTO battlepass_tiers (battlepass_id, tier_number, xp_required, is_premium, reward_type, reward_data)
VALUES
    (1, 10, 1000,  TRUE, 'CURRENCY', '{"amount": 200, "currency": "GOLD"}'),
    (1, 50, 5000,  TRUE, 'SKIN',     '{"skin_id": 3, "skin_name": "Chevalier de Minuit"}'),
    (1, 100, 10000, TRUE, 'SKIN',    '{"skin_id": 4, "skin_name": "Paladin de Cristal"}');

-- Objectifs saisonniers (NONE)
INSERT INTO objectives (season_id, name, description, objective_type, target_value, xp_reward, reset_type)
VALUES
    (1, 'Premier sang',       'Remporter votre premier duel',          'WIN_DUELS',            1,  100, 'NONE'),
    (1, 'Enchaîner les duels','Gagner 3 duels d''affilée',             'WIN_STREAK',           3,  500, 'NONE'),
    (1, 'Vétéran',            'Participer à 5 tournois',               'PLAY_TOURNAMENTS',     5,  800, 'NONE'),
    (1, 'Sans reproche',      'Terminer un tournoi sans pénalité',     'PLAY_WITHOUT_PENALTY', 1,  300, 'NONE'),
    (1, 'Champion saisonnier','Accumuler 100 points sur la saison',    'SCORE_POINTS',         100, 1500, 'NONE');

-- Objectifs journaliers (DAILY)
INSERT INTO objectives (season_id, name, description, objective_type, target_value, xp_reward, reset_type)
VALUES
    (1, 'Défi du jour — victoire', 'Remporter 1 duel aujourd''hui',    'WIN_DUELS', 1, 50, 'DAILY'),
    (1, 'Tournoi du jour',         'Participer à 1 tournoi aujourd''hui', 'PLAY_TOURNAMENTS', 1, 75, 'DAILY');

-- Objectifs hebdomadaires (WEEKLY)
INSERT INTO objectives (season_id, name, description, objective_type, target_value, xp_reward, reset_type)
VALUES
    (1, 'Semaine glorieuse', 'Remporter 5 duels cette semaine',        'WIN_DUELS',        5, 300, 'WEEKLY'),
    (1, 'Série magique',     'Déclencher le bonus de série 2 fois',    'EARN_BONUS',       2, 400, 'WEEKLY');

-- Récompenses de fin de saison
INSERT INTO season_rewards (season_id, rank_min, rank_max, reward_type, reward_data, label)
VALUES
    (1, 1,   1,    'SKIN',  '{"skin_id": 5, "skin_name": "Armure du Champion"}', 'Champion de la Saison'),
    (1, 2,   10,   'SKIN',  '{"skin_id": 6, "skin_name": "Armure de l''Élite"}', 'Top 10'),
    (1, 11,  50,   'TITLE', '{"title": "Chevalier de la Saison 1", "color": "#4169E1"}', 'Top 50'),
    (1, 51,  NULL, 'TITLE', '{"title": "Participant Saison 1", "color": "#808080"}', 'Participant');
```

---

## 8. Décisions de conception

| Décision | Raison |
|---|---|
| `period_key` au lieu de DELETE pour les resets daily/weekly | Conserve l'historique complet, permet l'audit et les stats de complétion |
| Index partiel `uq_seasonal_objective_completion WHERE period_key IS NULL` | Les objectifs saisonniers (NONE) ne peuvent être complétés qu'une fois — contrainte légère et ciblée |
| `reward_data JSONB` au lieu de FK vers `skins` | Un palier peut donner un skin, un titre, de la monnaie ou un boost. JSONB évite 4 tables nullable. |
| `win_streak_best` dans `seasonal_player_stats` | Nécessaire pour l'objectif `WIN_STREAK` sans recalcul coûteux sur les `match_results` |
| `season_rank` NULL jusqu'à clôture | Évite les ranks intermédiaires trompeurs — le rang n'existe que quand la saison est ENDED |
| Battlepass tiers : XP **cumulatif** | La requête "XP restant" est `xp_required - current_xp` — simple et sans agrégation |
| Skins de récompenses via `player_skin_inventory` (Phase 2) | Le système de récompenses délègue l'attribution des cosmétiques à la couche Phase 2 |

---

## 9. Migration Phase 2 → Phase 3

Toutes les tables Phase 1 et Phase 2 sont inchangées. Phase 3 s'ajoute sans migration destructive.

```sql
-- Script 03_phase3.sql (à ajouter dans docker/postgres/)
-- Exécuter après 02_phase2.sql
-- Contenu : section 4 ci-dessus (types + tables + index + seed)
```
