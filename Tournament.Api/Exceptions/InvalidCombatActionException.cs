namespace Tournament.Api.Exceptions;

public class InvalidCombatActionException : Exception
{
    public InvalidCombatActionException(string message) : base(message) { }
}
