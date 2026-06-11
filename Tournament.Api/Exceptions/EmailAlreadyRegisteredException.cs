namespace Tournament.Api.Exceptions;

public class EmailAlreadyRegisteredException : Exception
{
    public string Email { get; }

    public EmailAlreadyRegisteredException(string email)
        : base($"Email '{email}' is already registered.")
    {
        Email = email;
    }
}
