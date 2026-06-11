namespace Tournament.Api.Exceptions;

public class CombatNotFoundException : Exception
{
    public int CombatId { get; }

    public CombatNotFoundException(int combatId)
        : base($"Combat with id {combatId} was not found.")
    {
        CombatId = combatId;
    }
}
