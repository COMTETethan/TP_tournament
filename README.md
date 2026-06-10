# TP — Système de notation pour un tournoi d'escrime fantastique

Projet C# (.NET 10) de mise en pratique du TDD avec xUnit et FluentAssertions.

## Contexte

Backend d'un RPG fantastique : des chevaliers s'affrontent en duel. Le système calcule le score final de chaque participant selon des règles précises incluant un bonus de série, une disqualification et des pénalités.

## Règles du tournoi

| Résultat | Points |
|---|---|
| Victoire | +3 |
| Match nul | +1 |
| Défaite | 0 |
| Bonus de série | +5 si ≥ 3 victoires consécutives (une seule fois par série) |
| Disqualification | Score final = 0 |
| Pénalités | Soustraites du score, plancher à 0 |

## Structure

```
TP_TOURNAMENT/
├── Tournament.slnx
├── Tournament.Domain/          # Logique métier
│   └── Service/
│       ├── MatchResult.cs
│       ├── ScoreCalculator.cs
│       ├── Player.cs
│       └── TournamentRanking.cs
├── Tournament.UnitTests/       # Tests xUnit
│   ├── ScoreCalculatorTests.cs  (20+ tests)
│   └── TournamentRankingTests.cs (bonus)
└── docs/
    └── TEST_PLAN.md
```

## Approche TDD

**RED** → Les tests existent et échouent (`NotImplementedException`)  
**GREEN** → Implémenter `ScoreCalculator.CalculateScore` pour faire passer les tests  
**REFACTOR** → Nettoyer sans modifier le comportement

## Commandes

```bash
# Restaurer les dépendances
dotnet restore

# Lancer les tests
dotnet test

# Lancer les tests avec couverture
dotnet test --collect:"XPlat Code Coverage"

# Filtrer par exigence
dotnet test --filter "Requirement=REQ-T-004"

# Générer le rapport de couverture
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

## Auteurs

- Comtet Ethan
- Fevre Adrien
- Buanga Corentin
