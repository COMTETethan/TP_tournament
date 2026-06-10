# Plan de Test — Système de notation pour un tournoi d'escrime fantastique

---

## 1. Identification

| Champ | Valeur |
|---|---|
| Projet | Tournoi d'escrime fantastique |
| Version | 1.0 |
| Auteurs | Comtet Ethan, Fevre Adrien, Buanga Corentin |
| Formateur | KAKE Abdoulaye |
| Date de création | 2026-06-10 |
| Framework de test | xUnit 2.9.3 + FluentAssertions 6.12.0 |
| Environnement | .NET 10, C# 13 |

---

## 2. Périmètre et objectifs

### Ce qui est testé

- `ScoreCalculator.CalculateScore` : logique de calcul du score final (points de base, bonus de série, disqualification, pénalités, cas limites)
- `TournamentRanking.GetRanking` : classement des joueurs par score décroissant (bonus)
- `TournamentRanking.GetChampion` : sélection du champion (bonus)

### Ce qui est hors périmètre

- Persistance en base de données
- API REST
- Application web
- CI/CD GitHub Actions

### Objectifs pédagogiques

- Pratiquer le cycle TDD strict : RED → GREEN → REFACTOR
- Maîtriser `[Fact]`, `[Theory]`, `[InlineData]`, `[MemberData]`
- Utiliser FluentAssertions pour des assertions lisibles avec message d'explication
- Isoler les dépendances avec Moq (TournamentRanking)
- Atteindre une couverture ≥ 95 % sur `ScoreCalculator`

---

## 3. Stratégie de test

| Approche | Détail |
|---|---|
| Méthode | TDD — les tests sont écrits AVANT l'implémentation |
| Pattern | AAA : Arrange / Act / Assert, un seul concept par test |
| Nommage | `MethodName_Condition_ExpectedBehavior` |
| Traçabilité | Chaque test porte un `[Trait("Requirement", "REQ-T-XXX")]` |
| Paramétrisation | `[InlineData]` pour les cas simples, `[MemberData]` pour les scénarios complexes |
| Couverture cible | Lignes ≥ 95 %, Branches ≥ 90 % sur `ScoreCalculator` |

---

## 4. Critères d'entrée

- Solution compilable (`dotnet build` sans erreur)
- `ScoreCalculator.CalculateScore` lève `NotImplementedException` (phase RED)
- Tous les packages NuGet restaurés (`dotnet restore`)

---

## 5. Critères de sortie

- 100 % des tests passent (phase GREEN)
- Couverture lignes ≥ 95 % sur `ScoreCalculator`
- Couverture branches ≥ 90 % sur `ScoreCalculator`
- Zéro test ignoré ou skippé

---

## 6. Environnement

| Composant | Version |
|---|---|
| .NET SDK | 10.0 |
| xUnit | 2.9.3 |
| xunit.runner.visualstudio | 3.1.4 |
| FluentAssertions | 6.12.0 |
| Moq | 4.20.72 |
| coverlet.collector | 6.0.4 |
| OS | Linux (compatible Windows/macOS) |

---

## 7. Cas de test

### ScoreCalculator — Tests de base

| ID | Nom du test | REQ | Entrée | Attendu | Priorité |
|---|---|---|---|---|---|
| TC-001 | `CalculateScore_WinDrawLoss_ReturnsFour` | REQ-T-001, REQ-T-002, REQ-T-003 | W, D, L | 4 | Haute |
| TC-002 | `CalculateScore_TwoWins_ReturnsSix` | REQ-T-001 | W, W | 6 | Haute |
| TC-003 | `CalculateScore_ThreeDraws_ReturnsThree` | REQ-T-002 | D, D, D | 3 | Haute |
| TC-004 | `CalculateScore_TwoLosses_ReturnsZero` | REQ-T-003 | L, L | 0 | Haute |

### ScoreCalculator — Bonus de série

| ID | Nom du test | REQ | Entrée | Attendu | Priorité |
|---|---|---|---|---|---|
| TC-005 | `CalculateScore_ThreeConsecutiveWins_ReturnsFourteen` | REQ-T-004 | W×3 | 14 | Haute |
| TC-006 | `CalculateScore_FourConsecutiveWins_ReturnsSeventeen` | REQ-T-004 | W×4 | 17 | Haute |
| TC-007 | `CalculateScore_WinsInterrupted_NoBonus` | REQ-T-004 | W, W, L, W | 9 | Haute |
| TC-008 | `CalculateScore_TwoDistinctSeries_TwoBonuses` | REQ-T-005 | W×3, L, W×4 | 31 | Haute |
| TC-009 | `CalculateScore_DrawInterruptsStreak_NoBonus` | REQ-T-004 | W, D, W, W | 10 | Moyenne |

### ScoreCalculator — Disqualification

| ID | Nom du test | REQ | Entrée | Attendu | Priorité |
|---|---|---|---|---|---|
| TC-010 | `CalculateScore_Disqualified_ReturnsZero` | REQ-T-006 | W×3, disqualified=true | 0 | Haute |
| TC-011 | `CalculateScore_DisqualifiedWithNoMatches_ReturnsZero` | REQ-T-006 | [], disqualified=true | 0 | Moyenne |

### ScoreCalculator — Pénalités

| ID | Nom du test | REQ | Entrée | Attendu | Priorité |
|---|---|---|---|---|---|
| TC-012 | `CalculateScore_WithPenalty_SubtractsPenaltyPoints` | REQ-T-007 | W×4 (17 pts), penalty=3 | 14 | Haute |
| TC-013 | `CalculateScore_PenaltyExceedsScore_ReturnsZero` | REQ-T-008 | W, D (4 pts), penalty=8 | 0 | Haute |
| TC-014 | `CalculateScore_PenaltyEqualsScore_ReturnsZero` | REQ-T-008 | W, W, D (7 pts), penalty=7 | 0 | Haute |

### ScoreCalculator — Cas limites et exceptions

| ID | Nom du test | REQ | Entrée | Attendu | Priorité |
|---|---|---|---|---|---|
| TC-015 | `CalculateScore_EmptyList_ReturnsZero` | REQ-T-011 | [] | 0 | Haute |
| TC-016 | `CalculateScore_NullMatches_ThrowsArgumentNullException` | REQ-T-009 | null | ArgumentNullException("matches") | Haute |
| TC-017 | `CalculateScore_NegativePenalty_ThrowsArgumentException` | REQ-T-010 | [], penalty=-1 | ArgumentException("penaltyPoints") | Haute |
| TC-018 | `CalculateScore_LongTournament_CalculatesCorrectly` | REQ-T-001, REQ-T-003, REQ-T-004 | 100 matches W/L alternés | 150 | Basse |

### ScoreCalculator — Tests paramétrés

| ID | Nom du test | REQ | Paramètres | Priorité |
|---|---|---|---|---|
| TC-019 | `CalculateScore_VariousCombinations_ReturnsExpected` | REQ-T-001 à REQ-T-004 | (3,0,0)→14 ; (2,1,0)→7 ; (0,0,3)→0 | Moyenne |
| TC-020 | `CalculateScore_ComplexScenarios_ReturnsExpected` | REQ-T-004, REQ-T-005 | MemberData — 3 scénarios | Moyenne |

### TournamentRanking — Bonus

| ID | Nom du test | REQ | Entrée | Attendu | Priorité |
|---|---|---|---|---|---|
| TC-021 | `GetRanking_MultiplePlayers_SortedByScoreDescending` | REQ-T-012 | 3 joueurs scores différents | Ordre décroissant | Moyenne |
| TC-022 | `GetRanking_TiedPlayers_BothPresentInRanking` | REQ-T-012 | 2 joueurs même score | Les deux présents | Basse |
| TC-023 | `GetChampion_MultiplePlayers_ReturnsHighestScorePlayer` | REQ-T-013 | 3 joueurs scores différents | Joueur avec 14 pts | Moyenne |
| TC-024 | `GetChampion_AllDisqualified_ReturnsPlayerWithZeroScore` | REQ-T-013, REQ-T-006 | 2 joueurs disqualifiés | Score champion = 0 | Basse |

---

## 8. Matrice de traçabilité

| Exigence | Description | Cas de test | Statut |
|---|---|---|---|
| REQ-T-001 | Victoire = +3 points | TC-001, TC-002, TC-018, TC-019 | À valider |
| REQ-T-002 | Match nul = +1 point | TC-001, TC-003, TC-019 | À valider |
| REQ-T-003 | Défaite = 0 point | TC-001, TC-004, TC-018, TC-019 | À valider |
| REQ-T-004 | Bonus +5 pour 3+ victoires consécutives (une fois/série) | TC-005, TC-006, TC-007, TC-009, TC-018, TC-019, TC-020 | À valider |
| REQ-T-005 | Plusieurs séries → plusieurs bonus | TC-008, TC-020 | À valider |
| REQ-T-006 | Disqualification → score = 0 | TC-010, TC-011, TC-024 | À valider |
| REQ-T-007 | Pénalités soustraites du score | TC-012 | À valider |
| REQ-T-008 | Score final jamais négatif | TC-013, TC-014 | À valider |
| REQ-T-009 | null → ArgumentNullException | TC-016 | À valider |
| REQ-T-010 | penalty < 0 → ArgumentException | TC-017 | À valider |
| REQ-T-011 | Liste vide → 0 | TC-015 | À valider |
| REQ-T-012 | GetRanking trié par score décroissant | TC-021, TC-022 | À valider |
| REQ-T-013 | GetChampion retourne le meilleur score | TC-023, TC-024 | À valider |

---

## 9. Risques

| ID | Risque | Probabilité | Impact | Mitigation |
|---|---|---|---|---|
| R-001 | Confusion dans le comptage du bonus (une fois par série ou une fois pour tout le tournoi) | Haute | Haute | TC-008 couvre deux séries distinctes ; s'appuyer sur l'Exemple 3 du sujet (résultat = 31) |
| R-002 | Écriture de code avant les tests (violation TDD) | Moyenne | Haute | Vérifier que les tests échouent en phase RED avant toute implémentation |
| R-003 | Score négatif non géré (plancher à 0 oublié) | Moyenne | Haute | TC-013 et TC-014 testent explicitement ce cas limite |
| R-004 | Bonus accordé plusieurs fois sur la même série (Win×5 = 2 bonus) | Moyenne | Haute | TC-006 vérifie que Win×4 donne 17, pas 22 |
| R-005 | Logique dans les tests (boucles, conditions) | Basse | Moyenne | Garder les helpers W/D/L statiques et simples ; ne pas recoder la logique dans les tests |
| R-006 | Tests TournamentRanking dépendants de ScoreCalculator non implémenté | Haute | Moyenne | Les tests sont en phase RED jusqu'à ce que ScoreCalculator soit vert |

---

## 10. Responsabilités

| Rôle | Personne | Périmètre |
|---|---|---|
| Développeur / Testeur | Comtet Ethan | Implémentation, tests ScoreCalculator |
| Développeur / Testeur | Fevre Adrien | Implémentation, tests TournamentRanking (bonus) |
| Développeur / Testeur | Buanga Corentin | Revue, couverture, plan de test |
| Formateur | KAKE Abdoulaye | Validation finale |
