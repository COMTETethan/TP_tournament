namespace Tournament.Api.UnitTests.TestData;

/// <summary>
/// Jeu de données : outcomes de duels pour le joueur 1 (en tant que Player1) → score final attendu.
/// Règles : PLAYER1_WIN = +3 pts, DRAW = +1 pt, PLAYER2_WIN = -1 pt, plancher à 0, pénalité = -1 pt par point.
/// </summary>
public class PlayerScoreCases : TheoryData<string[], int, int, string>
{
    public PlayerScoreCases()
    {
        Add(["PLAYER1_WIN"],                                0, 3,  "une victoire");
        Add(["PLAYER1_WIN", "PLAYER1_WIN"],                 0, 6,  "deux victoires");
        Add(["PLAYER2_WIN"],                                0, 0,  "une défaite, plancher à 0");
        Add(["PLAYER2_WIN", "PLAYER2_WIN"],                 0, 0,  "deux défaites, plancher à 0");
        Add(["DRAW"],                                       0, 1,  "un nul");
        Add(["PLAYER1_WIN", "PLAYER2_WIN", "DRAW"],         0, 3,  "victoire + défaite + nul = 3");
        Add(["PLAYER1_WIN", "PLAYER1_WIN"],                 1, 5,  "deux victoires (6) - 1 pt de pénalité = 5");
        Add(["PLAYER2_WIN", "PLAYER2_WIN", "PLAYER2_WIN"],  1, 0,  "trois défaites avec pénalité, plancher à 0");
    }
}
