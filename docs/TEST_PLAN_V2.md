# Plan de Test — Tournoi d'escrime fantastique

---

## 1. Identification

| Champ | Valeur |
|---|---|
| Projet | Tournoi d'escrime fantastique (API + moteur de combat) |
| Version | 2.0 |
| Auteurs | Comtet Ethan, Fevre Adrien, Buanga Corentin |
| Formateur | KAKE Abdoulaye |
| Date de création | 2026-06-10 |
| Dernière mise à jour | 2026-06-11 |
| Framework de test | xUnit 2.9.3 + FluentAssertions 6.12.0 + Moq 4.20.72 |
| Environnement | .NET 10, C# 13 |

---

## 2. Périmètre et objectifs

### Ce qui est testé

- **Domaine** : `ScoreCalculator` (calcul du score) et `TournamentRanking` (classement, champion) — cœur TDD historique.
- **Services API** (logique métier in-memory) : tournois, champions (joueurs par user), inscriptions, duels, scores, combat tour-par-tour, replays, classes/skills, skins, saisons, battlepass, objectifs, récompenses, authentification JWT.
- **Contrôleurs API** : mapping requête → service → code HTTP (200/201/400/401/404/409) pour chaque endpoint.
- **Intégration** : parcours bout-en-bout (duel joué comme combat → score), démarrage de l'application, simulation de combat complet.

### Ce qui est hors périmètre (testé manuellement / en intégration)

- Persistance PostgreSQL (couche Dapper `Services/Db/*`, vérifiée en intégration contre un conteneur, marquée `[ExcludeFromCodeCoverage]`).
- Application web front-end.

### Objectifs

- Cycle TDD : RED → GREEN → REFACTOR.
- Maîtrise de `[Fact]`, `[Theory]`, `[InlineData]`, `[ClassData]`, `[MemberData]`, `[Trait]`.
- Assertions lisibles (FluentAssertions) et isolation des dépendances (Moq).
- Couverture de branches **≥ 95 %** (seuil CI GitHub Actions).

---

## 3. Stratégie de test

| Approche | Détail |
|---|---|
| Méthode | TDD — tests écrits avant l'implémentation |
| Pattern | **AAA** : chaque test est commenté `// Arrange` / `// Act` / `// Assert`, un seul concept par test |
| Nommage | `MethodName_Condition_ExpectedBehavior` |
| Traçabilité | `[Trait("Category", …)]` + `[Trait("Layer", …)]` sur chaque classe ; `[Trait("Requirement", "REQ-T-XXX")]` sur les tests du domaine |
| Paramétrisation | `[InlineData]` / `[ClassData]` / `[MemberData]` pour les scénarios multiples |
| Commentaires | Seuls les marqueurs AAA sont autorisés dans les fichiers de test |

---

## 4. Convention de `[Trait]`

Chaque classe de test porte deux traits, ce qui permet de filtrer l'exécution :

| Clé | Valeurs | Usage |
|---|---|---|
| `Category` | Domaine fonctionnel (`Combat`, `Auth`, `Tournament`, `Score`, …) | `dotnet test --filter "Category=Combat"` |
| `Layer` | `Domain`, `Service`, `Controller`, `Integration`, `Unit` | `dotnet test --filter "Layer=Controller"` |
| `Requirement` | `REQ-T-001`…`REQ-T-013` (domaine uniquement) | traçabilité fine du `ScoreCalculator` |

Exemples : `dotnet test --filter "Category=Combat"` (53), `dotnet test --filter "Layer=Controller"` (162),
`dotnet test --filter "Layer=Service&Category=Auth"`.

---

## 5. Suite de tests par catégorie

> **458 cas** au total exécutés (432 méthodes `[Fact]`/`[Theory]`, dont certaines paramétrées) :
> 426 dans `Tournament.Api.UnitTests`, 32 dans `Tournament.UnitTests` (domaine). 0 ignoré.

| Catégorie | Couches | Méthodes | Couvre |
|---|---|---|---|
| Combat | Service, Controller, Integration | 51 | moteur tour-par-tour, dégâts/défense/soin/aura, initiative, KO, replay |
| Skin | Service, Controller | 36 | loadouts joueurs, fonds de tournoi |
| Objective | Service, Controller | 34 | objectifs journaliers/hebdo, reset de période |
| Season | Service, Controller | 29 | saisons, transitions de statut, stats |
| Battlepass | Service, Controller | 27 | battlepass, tiers, XP |
| Replay | Service, Controller | 26 | enregistrement d'événements de duel, snapshots cosmétiques |
| Auth | Service, Controller | 25 | register/login/refresh JWT, `/me` |
| Registration | Service, Controller | 25 | inscription champion↔tournoi, DQ/pénalités par tournoi |
| SeasonReward | Service, Controller | 25 | récompenses de saison, distribution |
| Duel | Service, Controller | 24 | création de duel, issue, fin |
| DuelCombat | Service, Controller, Integration | 23 | orchestration duel↔combat, retour de l'issue + score |
| ScoreCalculator | Domain | 20 | calcul du score (voir §6) |
| Class | Service, Controller | 19 | roster classes/skills (lecture seule) |
| Score | Service, Controller | 18 | score par tournoi, classement, champion |
| Tournament | Service, Controller, Integration | 17 | CRUD tournoi, parcours 2 tournois |
| Player | Service, Controller | 17 | champions par user (création JWT, listing) |
| TournamentRanking | Domain | 8 | classement et champion (voir §6) |
| Dtos | Unit | 6 | égalité des records de réponse |
| Startup | Integration | 2 | démarrage de l'application |

---

## 6. Domaine — `ScoreCalculator` / `TournamentRanking` (traçabilité TDD)

### Cas de test principaux

| ID | Test | REQ | Attendu |
|---|---|---|---|
| TC-001 | `CalculateScore_WinDrawLoss_ReturnsFour` | REQ-T-001/002/003 | 4 |
| TC-005 | `CalculateScore_ThreeConsecutiveWins_ReturnsFourteen` | REQ-T-004 | 14 |
| TC-008 | `CalculateScore_TwoDistinctSeries_TwoBonuses` | REQ-T-005 | 31 |
| TC-010 | `CalculateScore_Disqualified_ReturnsZero` | REQ-T-006 | 0 |
| TC-013 | `CalculateScore_PenaltyExceedsScore_ReturnsZero` | REQ-T-008 | 0 |
| TC-016 | `CalculateScore_NullMatches_ThrowsArgumentNullException` | REQ-T-009 | ArgumentNullException |
| TC-021 | `GetRanking_MultiplePlayers_SortedByScoreDescending` | REQ-T-012 | ordre décroissant |
| TC-023 | `GetChampion_MultiplePlayers_ReturnsHighestScorePlayer` | REQ-T-013 | meilleur score |

### Matrice de traçabilité (domaine)

| Exigence | Description | Statut |
|---|---|---|
| REQ-T-001 | Victoire = +3 | ✅ |
| REQ-T-002 | Nul = +1 | ✅ |
| REQ-T-003 | Défaite = 0 | ✅ |
| REQ-T-004 | Bonus +5 / 3 victoires consécutives (une fois/série) | ✅ |
| REQ-T-005 | Plusieurs séries → plusieurs bonus | ✅ |
| REQ-T-006 | Disqualification → 0 | ✅ |
| REQ-T-007 | Pénalités soustraites | ✅ |
| REQ-T-008 | Score jamais négatif | ✅ |
| REQ-T-009 | `null` → `ArgumentNullException` | ✅ |
| REQ-T-010 | pénalité < 0 → `ArgumentException` | ✅ |
| REQ-T-011 | Liste vide → 0 | ✅ |
| REQ-T-012 | `GetRanking` trié décroissant | ✅ |
| REQ-T-013 | `GetChampion` = meilleur score | ✅ |

---

## 7. Critères de sortie

- 100 % des tests passent — **458/458 PASS** ✅
- Couverture de branches **≥ 95 %** (seuil CI) — **96,6 %** ✅
- Modules cœur (combat, champions, scores, inscriptions) à **100 % de lignes** ✅
- Zéro test ignoré ou skippé ✅
- Domaine `ScoreCalculator` / `TournamentRanking` : lignes et branches **100 %** ✅

---

## 8. Environnement

| Composant | Version |
|---|---|
| .NET SDK | 10.0 |
| xUnit / runner | 2.9.3 / 3.1.4 |
| FluentAssertions | 6.12.0 |
| Moq | 4.20.72 |
| coverlet.collector | 6.0.4 |
| ReportGenerator | global tool |
| OS | Linux (compatible Windows/macOS) |

---

## 9. Commandes utiles

```bash
# Toute la suite
dotnet test

# Par catégorie / couche
dotnet test --filter "Category=Combat"
dotnet test --filter "Layer=Controller"
dotnet test --filter "Layer=Service&Category=Auth"

# Couverture (seuil CI ≥ 95 % de branches)
dotnet test --settings coverage.runsettings --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:coveragereport -reporttypes:Html
```

---

## 10. Responsabilités

| Rôle | Personne | Périmètre |
|---|---|---|
| Développeur / Testeur | Comtet Ethan | Combat, champions, inscriptions, persistance |
| Développeur / Testeur | Fevre Adrien | Saisons, battlepass, objectifs, récompenses |
| Développeur / Testeur | Buanga Corentin | Persistance EF, revue, couverture |
| Formateur | KAKE Abdoulaye | Validation finale |
