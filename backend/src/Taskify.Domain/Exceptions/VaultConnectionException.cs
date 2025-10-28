namespace Taskify.Domain.Exceptions;

public class VaultConnectionException : Exception
{
    public VaultConnectionException(string message) : base(message) { }
    public VaultConnectionException(string message, Exception innerException)
        : base(message, innerException) { }
}