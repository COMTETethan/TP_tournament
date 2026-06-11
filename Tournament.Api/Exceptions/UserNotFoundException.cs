namespace Tournament.Api.Exceptions;

public class UserNotFoundException : Exception
{
    public int UserId { get; }

    public UserNotFoundException(int userId)
        : base($"User with id {userId} was not found.")
    {
        UserId = userId;
    }
}
