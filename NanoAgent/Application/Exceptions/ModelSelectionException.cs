namespace NanoAgent.Application.Exceptions;

public sealed class ModelSelectionException : ModelDiscoveryException
{
    public ModelSelectionException(string message)
        : base(message)
    {
    }
}
