namespace Tournament.Api.Exceptions;

public class BattlepassNotFoundException : Exception
{
    public int BattlepassId { get; }

    public BattlepassNotFoundException(int battlepassId)
        : base($"Battlepass with id {battlepassId} was not found.")
    {
        BattlepassId = battlepassId;
    }
}
