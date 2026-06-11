namespace Tournament.Api.Exceptions;

public class InvalidSkinCategoryException : Exception
{
    public string AttemptedCategory { get; }

    public InvalidSkinCategoryException(string attemptedCategory)
        : base($"'{attemptedCategory}' is not a valid skin category. Use 'PLAYER' or 'BACKGROUND'.")
    {
        AttemptedCategory = attemptedCategory;
    }
}
