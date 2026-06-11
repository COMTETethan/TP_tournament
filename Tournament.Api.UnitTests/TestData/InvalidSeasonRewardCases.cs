using Tournament.Api.DTOs.Requests;

namespace Tournament.Api.UnitTests.TestData;

/// <summary>
/// Jeu de données : configurations invalides pour CreateSeasonRewardAsync.
/// Chaque cas provoque une ArgumentException.
/// </summary>
public class InvalidSeasonRewardCases : TheoryData<CreateSeasonRewardRequest, string>
{
    public InvalidSeasonRewardCases()
    {
        Add(new CreateSeasonRewardRequest(0,    null, "SKIN", "{}", "Champion"), "RankMin = 0 (doit être >= 1)");
        Add(new CreateSeasonRewardRequest(5,    3,    "SKIN", "{}", "Invalid"),  "RankMax < RankMin");
        Add(new CreateSeasonRewardRequest(1,    null, "SKIN", "{}", ""),         "Label vide");
        Add(new CreateSeasonRewardRequest(1,    null, "SKIN", "{}", "   "),      "Label espaces uniquement");
    }
}
