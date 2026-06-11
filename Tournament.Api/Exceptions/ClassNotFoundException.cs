namespace Tournament.Api.Exceptions;

public class ClassNotFoundException : Exception
{
    public int ClassId { get; }

    public ClassNotFoundException(int classId)
        : base($"Class with id {classId} was not found.")
    {
        ClassId = classId;
    }
}
