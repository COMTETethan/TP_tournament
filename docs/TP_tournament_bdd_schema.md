# Schéma de Base de Données — Tournoi d'escrime fantastique

---

## 1. Vue d'ensemble

Le système doit persister :
- des **tournois** (un tournoi regroupe des joueurs et des duels)
- des **joueurs** (nom, disqualification, pénalités)
- des **duels** entre deux joueurs (avec un résultat)
- des **résultats individuels** par joueur (dérivés des duels, dans l'ordre chronologique — indispensable pour le calcul du bonus de série)

---

## 2. Entités

### `tournaments`
Un tournoi = un contexte d'affrontement (ex. : "Tournoi de la Table Ronde").

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | INT | PK, AUTO_INCREMENT | Identifiant unique |
| `name` | VARCHAR(150) | NOT NULL | Nom du tournoi |
| `status` | ENUM('OPEN','IN_PROGRESS','CLOSED') | NOT NULL, DEFAULT 'OPEN' | État du tournoi |
| `created_at` | DATETIME | NOT NULL, DEFAULT NOW() | Date de création |

---

### `players`
Un joueur participe à **un tournoi** (relation N joueurs → 1 tournoi).

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | INT | PK, AUTO_INCREMENT | Identifiant unique |
| `tournament_id` | INT | FK → tournaments.id, NOT NULL | Tournoi auquel appartient le joueur |
| `name` | VARCHAR(100) | NOT NULL | Nom du joueur (ex. : "Sir Galahad") |
| `is_disqualified` | BOOLEAN | NOT NULL, DEFAULT FALSE | Disqualification (score forcé à 0) |
| `penalty_points` | INT | NOT NULL, DEFAULT 0, CHECK >= 0 | Points de pénalité à soustraire |
| `created_at` | DATETIME | NOT NULL, DEFAULT NOW() | Date d'inscription |

---

### `duels`
Un duel oppose **deux joueurs** du même tournoi. C'est l'événement métier de base.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | INT | PK, AUTO_INCREMENT | Identifiant unique |
| `tournament_id` | INT | FK → tournaments.id, NOT NULL | Tournoi concerné |
| `player1_id` | INT | FK → players.id, NOT NULL | Premier joueur |
| `player2_id` | INT | FK → players.id, NOT NULL | Second joueur |
| `outcome` | ENUM('PLAYER1_WIN','PLAYER2_WIN','DRAW') | NOT NULL | Résultat du duel |
| `duel_order` | INT | NOT NULL | Position chronologique dans le tournoi |
| `played_at` | DATETIME | NOT NULL, DEFAULT NOW() | Horodatage du duel |

> `duel_order` est **critique** : le calcul du bonus de série dépend de l'ordre chronologique des victoires.

**Contrainte métier** : `player1_id <> player2_id` (un joueur ne peut pas s'affronter lui-même).

---

### `match_results` (vue dénormalisée dérivée de `duels`)
Une ligne = le résultat du point de vue **d'un joueur** pour un duel donné.  
Peut être une table matérialisée ou une vue SQL.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `id` | INT | PK, AUTO_INCREMENT | Identifiant unique |
| `player_id` | INT | FK → players.id, NOT NULL | Joueur concerné |
| `duel_id` | INT | FK → duels.id, NOT NULL | Duel source |
| `outcome` | ENUM('WIN','DRAW','LOSS') | NOT NULL | Résultat du point de vue du joueur |
| `match_order` | INT | NOT NULL | Copie de `duel_order` pour tri rapide |

> Cette table est le miroir direct de `List<MatchResult>` en C#. Elle permet de calculer le score sans reconstruire la liste à chaque fois.

---

### `scores` (cache calculé, optionnel)
Évite de recalculer le score à chaque lecture. Mise à jour à chaque modification de `match_results` ou de `players`.

| Colonne | Type | Contrainte | Description |
|---|---|---|---|
| `player_id` | INT | PK, FK → players.id | Joueur |
| `final_score` | INT | NOT NULL, DEFAULT 0, CHECK >= 0 | Score final calculé |
| `updated_at` | DATETIME | NOT NULL | Dernière mise à jour |

---

## 3. Diagramme Entité-Relation

```
tournaments
─────────────────────────────
PK  id             INT
    name           VARCHAR(150)
    status         ENUM
    created_at     DATETIME
         │
         │ 1
         │
         ▼ N
players
─────────────────────────────
PK  id             INT
FK  tournament_id  INT ───────────────────────────┐
    name           VARCHAR(100)                    │
    is_disqualified BOOLEAN                        │
    penalty_points INT                             │
    created_at     DATETIME                        │
         │                                         │
         │ 1                                       │
         │                      ┌──────────────────┘
         │                      │ 1
         ▼ N                    ▼ N
match_results             duels
──────────────────────    ────────────────────────────────
PK  id         INT        PK  id           INT
FK  player_id  INT ───►   FK  tournament_id INT
FK  duel_id    INT ───►   FK  player1_id    INT ──► players.id
    outcome    ENUM           player2_id    INT ──► players.id
    match_order INT           outcome       ENUM
                              duel_order    INT
                              played_at     DATETIME
         │
         │ 1
         ▼ 1
scores (cache)
──────────────────────
PK/FK  player_id    INT ──► players.id
       final_score  INT
       updated_at   DATETIME
```

---

## 4. Scripts SQL

### Création des tables

```sql
CREATE TABLE tournaments (
    id         INT          NOT NULL AUTO_INCREMENT,
    name       VARCHAR(150) NOT NULL,
    status     ENUM('OPEN', 'IN_PROGRESS', 'CLOSED') NOT NULL DEFAULT 'OPEN',
    created_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id)
);

CREATE TABLE players (
    id              INT          NOT NULL AUTO_INCREMENT,
    tournament_id   INT          NOT NULL,
    name            VARCHAR(100) NOT NULL,
    is_disqualified BOOLEAN      NOT NULL DEFAULT FALSE,
    penalty_points  INT          NOT NULL DEFAULT 0,
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    CONSTRAINT fk_players_tournament FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE,
    CONSTRAINT chk_penalty_non_negative CHECK (penalty_points >= 0)
);

CREATE TABLE duels (
    id            INT      NOT NULL AUTO_INCREMENT,
    tournament_id INT      NOT NULL,
    player1_id    INT      NOT NULL,
    player2_id    INT      NOT NULL,
    outcome       ENUM('PLAYER1_WIN', 'PLAYER2_WIN', 'DRAW') NOT NULL,
    duel_order    INT      NOT NULL,
    played_at     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    CONSTRAINT fk_duels_tournament FOREIGN KEY (tournament_id) REFERENCES tournaments(id) ON DELETE CASCADE,
    CONSTRAINT fk_duels_player1    FOREIGN KEY (player1_id)    REFERENCES players(id),
    CONSTRAINT fk_duels_player2    FOREIGN KEY (player2_id)    REFERENCES players(id),
    CONSTRAINT chk_different_players CHECK (player1_id <> player2_id)
);

CREATE TABLE match_results (
    id           INT  NOT NULL AUTO_INCREMENT,
    player_id    INT  NOT NULL,
    duel_id      INT  NOT NULL,
    outcome      ENUM('WIN', 'DRAW', 'LOSS') NOT NULL,
    match_order  INT  NOT NULL,
    PRIMARY KEY (id),
    CONSTRAINT fk_mr_player FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
    CONSTRAINT fk_mr_duel   FOREIGN KEY (duel_id)   REFERENCES duels(id)   ON DELETE CASCADE
);

CREATE TABLE scores (
    player_id   INT      NOT NULL,
    final_score INT      NOT NULL DEFAULT 0,
    updated_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (player_id),
    CONSTRAINT fk_scores_player FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE,
    CONSTRAINT chk_score_non_negative CHECK (final_score >= 0)
);
```

### Index recommandés

```sql
-- Accès rapide à tous les joueurs d'un tournoi
CREATE INDEX idx_players_tournament ON players(tournament_id);

-- Requêtes de classement : score décroissant par tournoi
CREATE INDEX idx_scores_player ON scores(player_id);

-- Accès aux résultats d'un joueur dans l'ordre chronologique
CREATE INDEX idx_mr_player_order ON match_results(player_id, match_order);

-- Accès aux duels d'un tournoi dans l'ordre
CREATE INDEX idx_duels_tournament_order ON duels(tournament_id, duel_order);
```

---

## 5. Requêtes clés

### Obtenir les résultats d'un joueur dans l'ordre (pour ScoreCalculator)
```sql
SELECT outcome
FROM   match_results
WHERE  player_id = :playerId
ORDER  BY match_order ASC;
```

### Classement d'un tournoi
```sql
SELECT p.name,
       s.final_score,
       p.is_disqualified
FROM   players p
JOIN   scores  s ON s.player_id = p.id
WHERE  p.tournament_id = :tournamentId
ORDER  BY s.final_score DESC;
```

### Champion d'un tournoi
```sql
SELECT p.name, s.final_score
FROM   players p
JOIN   scores  s ON s.player_id = p.id
WHERE  p.tournament_id = :tournamentId
ORDER  BY s.final_score DESC
LIMIT  1;
```

### Insérer un duel et ses deux match_results (transaction)
```sql
BEGIN;

INSERT INTO duels (tournament_id, player1_id, player2_id, outcome, duel_order)
VALUES (:tid, :p1, :p2, 'PLAYER1_WIN', :order);

SET @duel_id = LAST_INSERT_ID();

-- Résultat du point de vue de player1 (vainqueur)
INSERT INTO match_results (player_id, duel_id, outcome, match_order)
VALUES (:p1, @duel_id, 'WIN', :order);

-- Résultat du point de vue de player2 (perdant)
INSERT INTO match_results (player_id, duel_id, outcome, match_order)
VALUES (:p2, @duel_id, 'LOSS', :order);

COMMIT;
```

---

## 6. Mapping C# ↔ Base de données

| Classe C# | Table SQL | Notes |
|---|---|---|
| `Player` | `players` | `Matches` → jointure sur `match_results` |
| `Player.Matches` | `match_results` (triés par `match_order`) | L'ordre est critique pour le bonus de série |
| `Player.IsDisqualified` | `players.is_disqualified` | BOOLEAN |
| `Player.PenaltyPoints` | `players.penalty_points` | INT ≥ 0 |
| `MatchResult` | `match_results` | `Result.Win/Draw/Loss` ↔ `ENUM('WIN','DRAW','LOSS')` |
| `TournamentRanking` | Vue sur `scores` + `players` | Calculé par `ScoreCalculator`, mis en cache |
| *(pas de classe)* | `duels` | Niveau infrastructure — non exposé dans le domaine actuel |
| *(pas de classe)* | `scores` | Cache — recalculé après chaque duel |

---

## 7. Évolutions futures (selon le sujet)

| Évolution | Impact BDD |
|---|---|
| API REST | Pas d'impact schéma — ajouter couche repository + DTO |
| Notifications | Ajouter table `notifications(id, player_id, message, sent_at, status)` |
| Application web | Ajouter table `users(id, email, password_hash, player_id)` pour l'authentification |
| GitHub Actions CI | Ajouter base de test in-memory (SQLite) pour les tests d'intégration |
| Historique des scores | Transformer `scores` en `score_history(id, player_id, final_score, calculated_at)` |
