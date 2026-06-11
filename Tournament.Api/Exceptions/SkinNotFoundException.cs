namespace Tournament.Api.Exceptions;

public class SkinNotFoundException : Exception
{
    public int SkinId { get; }

    public SkinNotFoundException(int skinId)
        : base($"Skin with id {skinId} was not found.")
    {
        SkinId = skinId;
    }
}
