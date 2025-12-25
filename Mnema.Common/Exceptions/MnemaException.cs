namespace Mnema.Common.Exceptions;

public class MnemaException: Exception
{

    public MnemaException()
    {
    }

    public MnemaException(string? message): base(message)
    {
    }

    public MnemaException(string? message, Exception? innerException): base(message, innerException)
    {
    }

}