namespace BMS.Application.Exceptions;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message = "You do not have access to this resource.")
        : base(message) { }
}
