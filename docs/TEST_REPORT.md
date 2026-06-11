# Rapport d'exécution des tests — Tournoi d'escrime fantastique

---

## 1. Identification

| Champ | Valeur |
|---|---|
| Projet | Tournoi d'escrime fantastique (API + moteur de combat) |
| Version testée | 2.0 |
| Auteurs | Comtet Ethan, Fevre Adrien, Buanga Corentin |
| Formateur | KAKE Abdoulaye |
| Date d'exécution | 11/06/2026 |
| Durée totale d'exécution | ~2.5 s |

---

## 2. Résumé exécutif

| Métrique | Valeur |
|---|---|
| Nombre total de tests | 458 |
| Tests réussis | 458 (100 %) |
| Tests échoués | 0 |
| Tests ignorés | 0 |
| Couverture de lignes | 99.1 % (10 164 / 10 260) |
| Couverture de branches | 96.3 % (1 780 / 1 848) |
| **Verdict global** | **SUCCÈS** |

---

## 3. Résultats par catégorie

> Les 458 cas sont distribués en 432 méthodes `[Fact]`/`[Theory]` ; les méthodes paramétrées génèrent plusieurs instances.

| Catégorie | Layer(s) | Méthodes | Cas | Résultat | Durée estimée |
|---|---|---|---|---|---|
| Combat | Service, Controller, Integration | 51 | 53 | Réussi | < 1 ms — 87 ms |
| Skin | Service, Controller | 36 | 36 | Réussi | < 1 ms — 86 ms |
| Objective | Service, Controller | 34 | 35 | Réussi | < 1 ms — 89 ms |
| Season | Service, Controller | 29 | 30 | Réussi | < 1 ms — 86 ms |
| Battlepass | Service, Controller | 27 | 28 | Réussi | < 1 ms — 86 ms |
| Replay | Service, Controller | 26 | 27 | Réussi | < 1 ms — 87 ms |
| Auth | Service, Controller | 25 | 25 | Réussi | < 1 ms — 347 ms |
| Registration | Service, Controller | 25 | 25 | Réussi | < 1 ms |
| SeasonReward | Service, Controller | 25 | 25 | Réussi | < 1 ms — 86 ms |
| Duel | Service, Controller | 24 | 25 | Réussi | < 1 ms — 86 ms |
| DuelCombat | Service, Controller, Integration | 23 | 24 | Réussi | < 1 ms — 3 ms |
| ScoreCalculator | Domain | 20 | 20 | Réussi | < 1 ms — 3 ms |
| Class | Service, Controller | 19 | 20 | Réussi | < 1 ms — 87 ms |
| Score | Service, Controller | 18 | 18 | Réussi | < 1 ms — 86 ms |
| Tournament | Service, Controller, Integration | 17 | 17 | Réussi | < 1 ms |
| Player | Service, Controller | 17 | 17 | Réussi | < 1 ms — 86 ms |
| TournamentRanking | Domain | 8 | 12 | Réussi | < 1 ms — 30 ms |
| Dtos | Unit | 6 | 6 | Réussi | < 1 ms |
| Startup | Integration | 2 | 2 | Réussi | < 1 ms — 364 ms |

---

## 4. Résultats détaillés par exigence — domaine `ScoreCalculator` / `TournamentRanking`

| ID | Test | REQ | Résultat | Durée | Notes |
|---|---|---|---|---|---|
| TC-001 | `CalculateScore_WinDrawLoss_ReturnsFour` | REQ-T-001/002/003 | Réussi | < 1 ms | RAS |
| TC-002 | `CalculateScore_TwoWins_ReturnsSix` | REQ-T-001 | Réussi | < 1 ms | RAS |
| TC-003 | `CalculateScore_ThreeDraws_ReturnsThree` | REQ-T-002 | Réussi | < 1 ms | RAS |
| TC-004 | `CalculateScore_TwoLosses_ReturnsZero` | REQ-T-003 | Réussi | < 1 ms | RAS |
| TC-005 | `CalculateScore_ThreeConsecutiveWins_ReturnsFourteen` | REQ-T-004 | Réussi | < 1 ms | RAS |
| TC-006 | `CalculateScore_FourConsecutiveWins_ReturnsSeventeen` | REQ-T-004 | Réussi | < 1 ms | Série coupée à 3 puis reprend |
| TC-007 | `CalculateScore_WinsInterrupted_NoBonus` | REQ-T-004 | Réussi | < 1 ms | Draw brise la série |
| TC-008 | `CalculateScore_LongTournament_CalculatesCorrectly` | REQ-T-005 | Réussi | 1 ms | Deux séries → deux bonus |
| TC-009 | `CalculateScore_DrawInterruptsStreak_NoBonus` | REQ-T-005 | Réussi | < 1 ms | RAS |
| TC-010 | `CalculateScore_Disqualified_ReturnsZero` | REQ-T-006 | Réussi | < 1 ms | RAS |
| TC-011 | `CalculateScore_DisqualifiedWithNoMatches_ReturnsZero` | REQ-T-006 | Réussi | < 1 ms | RAS |
| TC-012 | `CalculateScore_WithPenalty_SubtractsPenaltyPoints` | REQ-T-007 | Réussi | < 1 ms | RAS |
| TC-013 | `CalculateScore_PenaltyExceedsScore_ReturnsZero` | REQ-T-008 | Réussi | < 1 ms | Pas de score négatif |
| TC-014 | `CalculateScore_PenaltyEqualsScore_ReturnsZero` | REQ-T-008 | Réussi | < 1 ms | RAS |
| TC-015 | `CalculateScore_EmptyList_ReturnsZero` | REQ-T-011 | Réussi | < 1 ms | RAS |
| TC-016 | `CalculateScore_NullMatches_ThrowsArgumentNullException` | REQ-T-009 | Réussi | 3 ms | RAS |
| TC-017 | `CalculateScore_NegativePenalty_ThrowsArgumentException` | REQ-T-010 | Réussi | < 1 ms | RAS |
| TC-018 | `MatchResult_DefaultConstructor_SetsDefaultOutcome` | — | Réussi | < 1 ms | Sanité du record |
| TC-019 | `GetRanking_MultiplePlayers_SortedByScoreDescending` | REQ-T-012 | Réussi | 16 ms | RAS |
| TC-020 | `GetRanking_TiedPlayers_BothPresentInRanking` | REQ-T-012 | Réussi | 2 ms | RAS |
| TC-021 | `GetRanking_NullPlayers_ThrowsArgumentNullException` | REQ-T-012 | Réussi | 30 ms | RAS |
| TC-022 | `GetChampion_MultiplePlayers_ReturnsHighestScorePlayer` | REQ-T-013 | Réussi | 3 ms | RAS |
| TC-023 | `GetChampion_AllDisqualified_ReturnsPlayerWithZeroScore` | REQ-T-013 | Réussi | 2 ms | Plus haut score = 0 |
| TC-024 | `GetChampion_EmptyPlayersList_ThrowsInvalidOperationException` | REQ-T-013 | Réussi | < 1 ms | RAS |
| TC-025 | `GetChampion_NullPlayers_ThrowsArgumentNullException` | REQ-T-013 | Réussi | < 1 ms | RAS |

---

## 5. Anomalies détectées

Aucune anomalie bloquante. Observations mineures :

- **OBS-001** : Tests Auth BCrypt volontairement lents
  - Sévérité : Information
  - Statut : Accepté (by design)
  - Description : `LoginAsync_ValidCredentials` (347 ms), `RegisterAsync_DuplicateEmail` (292 ms) et `RefreshAsync` (~90 ms) prennent plusieurs centaines de millisecondes à cause du work factor BCrypt par défaut (coût 11). Ce comportement est intentionnel — il reflète le vrai coût de hachage en production. Ce n'est pas un bug.

- **OBS-002** : Couche Dapper exclue de la couverture instrumentée
  - Sévérité : Information
  - Statut : Accepté (périmètre délibéré)
  - Description : Les classes sous `Services/Db/` (`DbTournamentService`, `DbPlayerService`, `DbDuelService`, `DbCombatPersistenceDecorator`, etc.) portent `[ExcludeFromCodeCoverage]`. Elles sont vérifiées manuellement via le conteneur PostgreSQL. Leur exclusion est documentée dans le TEST_PLAN.

- **OBS-003** : Frontend ajouté hors périmètre de test automatisé
  - Sévérité : Information
  - Statut : Hors périmètre
  - Description : Une application front-end a été ajoutée au projet. Elle n'est pas couverte par la suite xUnit. Des tests end-to-end (Playwright, Cypress) ou manuels restent à planifier si nécessaire.

---

## 6. Métriques détaillées

### Distribution par type

| Type | Nombre |
|---|---|
| Tests nominaux (`[Fact]`) | 380 |
| Tests d'erreur (exceptions, `[Fact]`) | 52 |
| Tests paramétrés (`[Theory]`, 26 méthodes) | 26 instances supplémentaires |
| **Total cas exécutés** | **458** |
| Tests dans `Tournament.UnitTests` (domaine) | 32 |
| Tests dans `Tournament.Api.UnitTests` (API) | 426 |

### Performance

| Métrique | Valeur |
|---|---|
| Test le plus rapide | < 1 ms (majorité) |
| Test le plus lent | 364 ms (`App_StartsAndRespondsToHealthCheck`) |
| Second plus lent | 347 ms (`LoginAsync_ValidCredentials_ReturnsAuthResponse`) |
| Troisième plus lent | 292 ms (`RegisterAsync_DuplicateEmail_ThrowsEmailAlreadyRegisteredException`) |
| Durée moyenne par test | ~5.5 ms |
| Durée totale de la suite | ~2.5 s |

---

## 7. Analyse de la couverture

| Métrique | Valeur |
|---|---|
| Lignes couvertes | 10 164 / 10 260 (99.1 %) |
| Branches couvertes | 1 780 / 1 848 (96.3 %) |
| Méthodes non couvertes | Aucune (hors `ExcludeFromCodeCoverage`) |

### Branches non couvertes

Les 68 branches non couvertes (3.7 %) se répartissent principalement en trois catégories :

- **Services Dapper exclus** : les classes sous `Services/Db/*` portent `[ExcludeFromCodeCoverage]` — leur couverture n'est pas instrumentée par choix d'architecture (tests d'intégration séparés contre PostgreSQL).
- **Guards défensifs dans les contrôleurs** : `TryGetUserId` possède un chemin d'échec pour token JWT mal formé (hors format attendu) qui n'est pas simulé par les tests unitaires de contrôleurs existants.
- **Fallbacks de catalogues scellés** : les expressions `switch` de `ClassCatalog` et `CombatService` ont des branches `_ => throw` structurellement inaccessibles via l'API publique — les entrées sont des constantes de compilation.

### Objectif CI

Le seuil GitHub Actions est fixé à **≥ 95 % de branches**. La suite atteint **96.3 %**, soit 1.3 point au-dessus du seuil.

---

## 8. Difficultés rencontrées

Le cycle Red-Green-Refactor a été respecté pour chaque fonctionnalité. Principales difficultés rencontrées :

- **Moteur de combat tour-par-tour** : La résolution en deux phases (buffs/debuffs d'abord, puis attaques par ordre d'initiative) a nécessité plusieurs cycles TDD pour aligner les valeurs exactes de HP attendues avec la logique de priorité. Les cas limites (KO avant que le perdant frappe, guérison au-delà du MaxHp, durée des auras) ont chacun nécessité un test dédié en amont de l'implémentation.

- **Isolation des stores statiques** : `CombatService` utilise un `static readonly List<CombatEntity>` partagé entre les tests. La solution retenue — une instance dédiée `new CombatService()` et des ID uniques par test — fonctionne mais représente une contrainte de conception à documenter pour les futurs contributeurs.

- **Orchestration duel ↔ combat** : `DuelCombatService` propage l'issue du combat (PLAYER1_WIN/PLAYER2_WIN) et la durée au duel une fois le combat terminé. Tester cette intégration avec Moq a nécessité de configurer les callbacks dans l'ordre exact de finalization, et d'utiliser `ReturnsAsync(() => _duel)` (lambda) pour capturer la valeur mutée par le callback.

- **Champions par utilisateur (multi-tournoi)** : La dissociation entre `players.user_id` (propriété JWT) et `tournament_players` (inscription par tournoi) a demandé une refonte du service de score pour lire les DQ/pénalités par tournoi, avec des tests couvrant les deux dimensions indépendamment.

- **Persistance Dapper** : L'intégration des types PostgreSQL natifs (enums `duel_outcome`, `INTERVAL` pour la durée) avec Dapper a nécessité des casts SQL explicites (`::duel_outcome`, `make_interval(secs => …)`) qui ne peuvent pas être testés unitairement — d'où le choix d'exclure cette couche de la couverture automatisée et de la valider manuellement contre un conteneur PostgreSQL.

---

## 9. Conclusion et recommandations

Les 13 exigences fonctionnelles du domaine (REQ-T-001 à REQ-T-013) sont entièrement couvertes. La suite étendue couvre également le moteur de combat, les champions (joueurs par user), les inscriptions, les scores, l'authentification JWT, les saisons, le battlepass, les objectifs, les récompenses et les replays. **458/458 tests passent**, avec une couverture de branches à **96.3 %** (seuil CI : 95 %).

**Recommandations :**

- Ajouter des tests end-to-end pour le frontend (Playwright ou Cypress) afin de couvrir les parcours utilisateur complets.
- Envisager des tests d'intégration automatisés contre PostgreSQL (via Testcontainers) pour remplacer la vérification manuelle des services Dapper.
- Remplacer les stores statiques de `CombatService` par une instance propre par test (via injection) pour éliminer la contrainte d'isolation et permettre de paralléliser les tests.
- Le seuil CI de 95 % est atteint avec marge — envisager de le porter à 97 % pour forcer la couverture des branches défensives restantes.

---

## 10. Signature

| Champ | Valeur |
|---|---|
| Auteurs du rapport | Comtet Ethan, Fevre Adrien, Buanga Corentin |
| Validé par | À remplir par le formateur |
