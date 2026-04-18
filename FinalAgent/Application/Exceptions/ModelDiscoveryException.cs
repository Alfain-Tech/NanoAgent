namespace FinalAgent.Application.Exceptions;

public class ModelDiscoveryException : InvalidOperationException
{
    public ModelDiscoveryException(string message)
        : base(message)
    {
    }

    public ModelDiscoveryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
