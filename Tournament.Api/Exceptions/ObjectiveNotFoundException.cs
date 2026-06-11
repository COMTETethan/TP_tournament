namespace Tournament.Api.Exceptions;

public class ObjectiveNotFoundException : Exception
{
    public int ObjectiveId { get; }

    public ObjectiveNotFoundException(int objectiveId)
        : base($"Objective with id {objectiveId} was not found.")
    {
        ObjectiveId = objectiveId;
    }
}
