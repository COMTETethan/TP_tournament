namespace Tournament.Api.UnitTests.TestData;

public class InvalidDuelOutcomeCases : TheoryData<string>
{
    public InvalidDuelOutcomeCases()
    {
        Add("BANANA");     // valeur inexistante
        Add("   ");        // espaces uniquement
        Add("");           // chaîne vide
        Add("WIN");        // valeur tronquée
        Add("LOSE");       // terme incorrect
    }
}
