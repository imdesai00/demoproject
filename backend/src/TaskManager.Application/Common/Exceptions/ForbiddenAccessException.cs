namespace TaskManager.Application.Common.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message = "You do not have permission to access this resource.")
        : base(message) { }
}
