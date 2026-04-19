namespace FinalAgent.Application.Exceptions;

public sealed class ConversationProviderException : Exception
{
    public ConversationProviderException(string message)
        : base(message)
    {
    }

    public ConversationProviderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
