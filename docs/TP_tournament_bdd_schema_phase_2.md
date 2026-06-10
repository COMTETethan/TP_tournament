# Schéma BDD Phase 2 — Replay, Skins joueurs, Skins background

---

## 1. Principe de conception fondamental

> **Les skins ne doivent jamais contaminer les données de replay.**

Un replay est une reconstitution fidèle d'un duel passé. Il doit rester rejouable indépendamment de :
- La suppression d'un skin
- La mise à jour d'un skin
- La déconnexion d'un joueur

**Stratégie retenue :** les événements de replay ne référencent aucun skin. Les skins actifs au moment du duel sont snapshotés dans une table séparée (`duel_cosmetic_snapshots`). À l'affichage, le frontend superpose le snapshot cosmétique au-dessus du replay événementiel. Les deux couches sont indépendantes.

```
REPLAY          = événements de jeu (pur, immuable, sans cosmétique)
SKIN SNAPSHOT   = apparence au moment du duel (peut être NULL si manquant)
SKINS CATALOG   = référentiel des assets disponibles (peut évoluer)
```

---

## 2. Nouvelles tables

### `duel_replays`
Métadonnées du replay d'un duel.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | SERIAL | PK | Identifiant unique |
| `duel_id` | INT | FK → duels.id, UNIQUE, NOT NULL | Un replay par duel |
| `schema_version` | SMALLINT | NOT NULL, DEFAULT 1 | Version du format d'événements (permet la migration future) |
| `recorded_at` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Quand le replay a été enregistré |
| `is_complete` | BOOLEAN | NOT NULL, DEFAULT FALSE | FALSE si le duel est encore en cours |

> `schema_version` est critique : si le format des événements change en v2, les anciens replays restent lisibles car on connaît leur version.

---

### `duel_replay_events`
La séquence brute d'événements d'un duel, dans l'ordre chronologique absolu.  
Aucune référence à des assets cosmétiques.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | BIGSERIAL | PK | Identifiant unique |
| `replay_id` | INT | FK → duel_replays.id, NOT NULL | Replay parent |
| `event_order` | INT | NOT NULL | Position dans la séquence |
| `event_type` | event_type (ENUM) | NOT NULL | Type d'événement (voir ci-dessous) |
| `actor_player_id` | INT | FK → players.id, NULLABLE | Joueur qui déclenche l'action |
| `target_player_id` | INT | FK → players.id, NULLABLE | Joueur cible (NULL si événement global) |
| `occurred_at_ms` | INT | NOT NULL | Millisecondes depuis le début du duel (`played_at`) |
| `payload` | JSONB | NULLABLE | Données spécifiques à l'événement (sans cosmétiques) |

**Enum `event_type` :**
```sql
CREATE TYPE event_type AS ENUM (
    'DUEL_START',
    'ATTACK',
    'PARRY',
    'TOUCH',       -- point marqué
    'PENALTY',     -- pénalité attribuée
    'TIMEOUT',
    'DISQUALIFY',
    'DUEL_END'
);
```

**Exemples de `payload` (JSONB) :**
```jsonc
// ATTACK
{ "direction": "HIGH", "weapon_hand": "RIGHT" }

// TOUCH
{ "zone": "TORSO", "points_scored": 1 }

// PENALTY
{ "reason": "CHEATING", "points_removed": 2 }
```

> Le JSONB est intentionnellement flexible : les champs varient par `event_type`. Il ne contient jamais de référence à un skin ou asset.

---

### `skins`
Catalogue de tous les assets cosmétiques disponibles.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | SERIAL | PK | Identifiant unique |
| `category` | skin_category (ENUM) | NOT NULL | Type de skin |
| `name` | VARCHAR(100) | NOT NULL, UNIQUE | Nom lisible (ex. "Paladin doré") |
| `asset_key` | VARCHAR(200) | NOT NULL, UNIQUE | Clé technique vers le fichier asset |
| `is_premium` | BOOLEAN | NOT NULL, DEFAULT FALSE | Skin payant ou non |
| `is_active` | BOOLEAN | NOT NULL, DEFAULT TRUE | Soft-delete : ne supprime jamais un skin référencé |
| `created_at` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Date d'ajout |

**Enum `skin_category` :**
```sql
CREATE TYPE skin_category AS ENUM (
    'PLAYER',       -- apparence du personnage
    'BACKGROUND'    -- décor de l'arène
);
```

---

### `player_skin_loadouts`
Le skin actif d'un joueur (un seul skin PLAYER actif à la fois).

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | SERIAL | PK | Identifiant unique |
| `player_id` | INT | FK → players.id, NOT NULL | Joueur |
| `skin_id` | INT | FK → skins.id, NOT NULL | Skin équipé |
| `equipped_at` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Quand le skin a été équipé |

**Contrainte :** un joueur ne peut avoir qu'un skin de catégorie PLAYER actif. Géré en application ou via une contrainte partielle.

---

### `tournament_background_loadouts`
Le skin de background actif pour un tournoi.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `tournament_id` | INT | PK, FK → tournaments.id | Tournoi |
| `skin_id` | INT | FK → skins.id, NOT NULL | Skin BACKGROUND actif |
| `set_at` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Quand le background a été défini |

---

### `duel_cosmetic_snapshots`
**La pièce maîtresse de l'isolation replay/skin.**  
Snapshot de l'apparence des deux joueurs et du background **au moment du duel**.  
Stocke les `asset_key` sous forme de chaînes — pas de FK — pour survivre à une suppression de skin.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `duel_id` | INT | PK, FK → duels.id | Duel concerné |
| `player1_skin_name` | VARCHAR(100) | NULLABLE | Nom du skin joueur 1 au moment du duel |
| `player1_asset_key` | VARCHAR(200) | NULLABLE | Clé asset joueur 1 (copie, pas FK) |
| `player2_skin_name` | VARCHAR(100) | NULLABLE | Nom du skin joueur 2 au moment du duel |
| `player2_asset_key` | VARCHAR(200) | NULLABLE | Clé asset joueur 2 (copie, pas FK) |
| `background_skin_name` | VARCHAR(100) | NULLABLE | Nom du background au moment du duel |
| `background_asset_key` | VARCHAR(200) | NULLABLE | Clé asset background (copie, pas FK) |
| `snapshotted_at` | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() | Quand le snapshot a été pris |

> Toutes les colonnes sont NULLABLE : si un joueur n'a pas de skin, le frontend affiche le skin par défaut.  
> Les clés sont copiées : si le skin est supprimé du catalogue, le snapshot reste intact.

---

## 3. Diagramme Entité-Relation — Phase 2 complète

```
tournaments ──────────────────────────────────────────────────────────┐
    │                                                                  │
    │ 1                                                        1       │
    ▼ N                                                        ▼ 1    │
players                                              tournament_background_loadouts
    │    \                                                      │
    │     \──────────────────────────────────────────► skins ◄─┘
    │      (player_skin_loadouts)                        ▲
    │ 1                                                  │ (soft ref via asset_key)
    ▼ N                                                  │
match_results                                            │
                                                         │
duels ───────────────────────────────────────────────────┤
    │                                                     │
    │ 1                                       1           │
    ├──────────────────────────────────► duel_cosmetic_snapshots
    │                                    (asset_key = string copy, no FK)
    │ 1
    ▼ 1
duel_replays
    │ 1
    ▼ N
duel_replay_events
    (event_type, actor_player_id, target_player_id, occurred_at_ms, payload JSONB)
    (NO reference to skins)
```

---

## 4. Scripts SQL — Phase 2

```sql
-- ── New enum types ─────────────────────────────────────────────

DO $$ BEGIN
    CREATE TYPE event_type AS ENUM (
        'DUEL_START', 'ATTACK', 'PARRY', 'TOUCH',
        'PENALTY', 'TIMEOUT', 'DISQUALIFY', 'DUEL_END'
    );
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
    CREATE TYPE skin_category AS ENUM ('PLAYER', 'BACKGROUND');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

-- ── Replay ─────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS duel_replays (
    id             SERIAL      NOT NULL,
    duel_id        INT         NOT NULL,
    schema_version SMALLINT    NOT NULL DEFAULT 1,
    recorded_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_complete    BOOLEAN     NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_duel_replays   PRIMARY KEY (id),
    CONSTRAINT uq_replay_duel    UNIQUE (duel_id),
    CONSTRAINT fk_replay_duel    FOREIGN KEY (duel_id) REFERENCES duels(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS duel_replay_events (
    id               BIGSERIAL   NOT NULL,
    replay_id        INT         NOT NULL,
    event_order      INT         NOT NULL,
    event_type       event_type  NOT NULL,
    actor_player_id  INT,
    target_player_id INT,
    occurred_at_ms   INT         NOT NULL,
    payload          JSONB,
    CONSTRAINT pk_replay_events  PRIMARY KEY (id),
    CONSTRAINT fk_re_replay      FOREIGN KEY (replay_id)        REFERENCES duel_replays(id) ON DELETE CASCADE,
    CONSTRAINT fk_re_actor       FOREIGN KEY (actor_player_id)  REFERENCES players(id),
    CONSTRAINT fk_re_target      FOREIGN KEY (target_player_id) REFERENCES players(id),
    CONSTRAINT uq_re_order       UNIQUE (replay_id, event_order)
);

CREATE INDEX IF NOT EXISTS idx_replay_events_order
    ON duel_replay_events(replay_id, event_order);

-- ── Skins catalog ─────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS skins (
    id         SERIAL        NOT NULL,
    category   skin_category NOT NULL,
    name       VARCHAR(100)  NOT NULL,
    asset_key  VARCHAR(200)  NOT NULL,
    is_premium BOOLEAN       NOT NULL DEFAULT FALSE,
    is_active  BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_skins       PRIMARY KEY (id),
    CONSTRAINT uq_skin_name   UNIQUE (name),
    CONSTRAINT uq_skin_asset  UNIQUE (asset_key)
);

-- ── Player skin loadout ────────────────────────────────────────

CREATE TABLE IF NOT EXISTS player_skin_loadouts (
    id          SERIAL      NOT NULL,
    player_id   INT         NOT NULL,
    skin_id     INT         NOT NULL,
    equipped_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_loadout        PRIMARY KEY (id),
    CONSTRAINT fk_loadout_player FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
    CONSTRAINT fk_loadout_skin   FOREIGN KEY (skin_id)   REFERENCES skins(id)
);

-- Only one active PLAYER skin per player (enforced at application layer or via partial index)
CREATE UNIQUE INDEX IF NOT EXISTS uq_one_player_skin
    ON player_skin_loadouts(player_id)
    WHERE skin_id IN (SELECT id FROM skins WHERE category = 'PLAYER' AND is_active = TRUE);

-- ── Tournament background loadout ─────────────────────────────

CREATE TABLE IF NOT EXISTS tournament_background_loadouts (
    tournament_id INT         NOT NULL,
    skin_id       INT         NOT NULL,
    set_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_bg_loadout        PRIMARY KEY (tournament_id),
    CONSTRAINT fk_bg_tournament     FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE,
    CONSTRAINT fk_bg_skin           FOREIGN KEY (skin_id)       REFERENCES skins(id)
);

-- ── Cosmetic snapshot (replay isolation) ──────────────────────

CREATE TABLE IF NOT EXISTS duel_cosmetic_snapshots (
    duel_id               INT          NOT NULL,
    player1_skin_name     VARCHAR(100),
    player1_asset_key     VARCHAR(200),
    player2_skin_name     VARCHAR(100),
    player2_asset_key     VARCHAR(200),
    background_skin_name  VARCHAR(100),
    background_asset_key  VARCHAR(200),
    snapshotted_at        TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_cosmetic_snapshot PRIMARY KEY (duel_id),
    CONSTRAINT fk_cs_duel           FOREIGN KEY (duel_id) REFERENCES duels(id) ON DELETE CASCADE
);
```

---

## 5. Requêtes clés

### Charger un replay complet (événements + snapshot cosmétique)
```sql
SELECT
    e.event_order,
    e.event_type,
    e.occurred_at_ms,
    e.actor_player_id,
    e.target_player_id,
    e.payload,
    -- Cosmetic snapshot (separate layer, may be NULL)
    cs.player1_asset_key,
    cs.player2_asset_key,
    cs.background_asset_key
FROM   duel_replay_events e
JOIN   duel_replays        r  ON r.id       = e.replay_id
LEFT   JOIN duel_cosmetic_snapshots cs ON cs.duel_id = r.duel_id
WHERE  r.duel_id = :duelId
ORDER  BY e.event_order ASC;
```

### Prendre un snapshot cosmétique avant le début d'un duel
```sql
INSERT INTO duel_cosmetic_snapshots (
    duel_id,
    player1_skin_name,  player1_asset_key,
    player2_skin_name,  player2_asset_key,
    background_skin_name, background_asset_key
)
SELECT
    :duelId,
    s1.name,  s1.asset_key,
    s2.name,  s2.asset_key,
    sb.name,  sb.asset_key
FROM duels d
-- Skin joueur 1
LEFT JOIN player_skin_loadouts  l1 ON l1.player_id = d.player1_id
LEFT JOIN skins                 s1 ON s1.id = l1.skin_id AND s1.category = 'PLAYER'
-- Skin joueur 2
LEFT JOIN player_skin_loadouts  l2 ON l2.player_id = d.player2_id
LEFT JOIN skins                 s2 ON s2.id = l2.skin_id AND s2.category = 'PLAYER'
-- Background du tournoi
LEFT JOIN tournament_background_loadouts tbl ON tbl.tournament_id = d.tournament_id
LEFT JOIN skins                          sb  ON sb.id = tbl.skin_id
WHERE d.id = :duelId
ON CONFLICT (duel_id) DO NOTHING;   -- idempotent
```

### Lister les replays disponibles d'un tournoi
```sql
SELECT
    d.id         AS duel_id,
    d.played_at,
    d.duration,
    d.played_at + d.duration AS ended_at,
    p1.name      AS player1,
    p2.name      AS player2,
    d.outcome,
    dr.is_complete,
    dr.schema_version
FROM   duels         d
JOIN   players       p1 ON p1.id = d.player1_id
JOIN   players       p2 ON p2.id = d.player2_id
JOIN   duel_replays  dr ON dr.duel_id = d.id
WHERE  d.tournament_id = :tournamentId
  AND  dr.is_complete = TRUE
ORDER  BY d.duel_order ASC;
```

---

## 6. Flux d'un duel complet

```
1. Duel créé          → INSERT INTO duels (played_at = NOW(), duration = NULL, outcome = NULL)
2. Snapshot cosmétic  → INSERT INTO duel_cosmetic_snapshots (asset_key copiés, pas de FK)
3. Replay créé        → INSERT INTO duel_replays (is_complete = FALSE)
4. Événements         → INSERT INTO duel_replay_events (event_type, occurred_at_ms, payload)
                        ... stream d'événements en cours de duel ...
5. Duel terminé       → UPDATE duels SET outcome = 'PLAYER1_WIN', duration = NOW() - played_at
                        UPDATE duel_replays SET is_complete = TRUE
6. Résultats          → INSERT INTO match_results (x2)
7. Score recalculé    → UPSERT INTO scores
```

---

## 7. Décisions de conception importantes

| Décision | Raison |
|---|---|
| `duel_cosmetic_snapshots` stocke des chaînes, pas des FK | Un skin supprimé ne casse pas le replay |
| `payload` JSONB dans `duel_replay_events` | Flexible pour de nouveaux types d'événements sans ALTER TABLE |
| `schema_version` dans `duel_replays` | Permet de migrer le format d'événements sans réécrire les anciens replays |
| `is_active` dans `skins` (soft delete) | On ne supprime jamais un skin : on le désactive. Les loadouts restent cohérents |
| Replay ≠ skins (couches séparées) | Le replay est rejoué fidèlement même si tous les skins sont supprimés |
| `occurred_at_ms` (entier, ms depuis played_at) | Compatible frontend JS/Unity sans conversion INTERVAL |

---

## 8. Ce qui est prévu mais non encore modélisé

| Feature future | Impact BDD anticipé |
|---|---|
| Inventaire de skins par joueur | Table `player_skin_inventory(player_id, skin_id, acquired_at)` |
| Skins premium / achat | Table `skin_purchases(id, player_id, skin_id, price, purchased_at)` |
| Vitesse de replay (×1, ×2, ×0.5) | Aucun impact BDD — calculé côté frontend via `occurred_at_ms` |
| Replay partageable (URL publique) | Ajouter `public_token UUID UNIQUE` dans `duel_replays` |
| Skins animés (vs statiques) | Ajouter `is_animated BOOLEAN` dans `skins` |
| Spectateurs en live | Table `duel_spectators(duel_id, user_id, joined_at)` |

---

## 9. Migration Phase 1 → Phase 2

Les tables Phase 1 ne sont pas modifiées. Phase 2 s'ajoute par-dessus.

```sql
-- Script de migration (02_phase2.sql à ajouter dans docker/postgres/)
-- Exécuté manuellement après la Phase 1 ou via un outil de migration (Flyway, EF Migrations)

-- 1. Nouveaux types enum
-- 2. Nouvelles tables (voir section 4)
-- 3. Données de seed (skins par défaut)

INSERT INTO skins (category, name, asset_key, is_premium) VALUES
    ('PLAYER',     'Chevalier classique', 'skins/player/classic_knight',   FALSE),
    ('PLAYER',     'Paladin doré',        'skins/player/golden_paladin',    TRUE),
    ('BACKGROUND', 'Arène de pierre',     'skins/bg/stone_arena',           FALSE),
    ('BACKGROUND', 'Château fantôme',     'skins/bg/ghost_castle',          TRUE)
ON CONFLICT DO NOTHING;
```
