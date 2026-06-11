namespace Tournament.Api.Exceptions;

public class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException()
        : base("Refresh token is invalid or has expired.") { }
}
