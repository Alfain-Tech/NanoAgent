namespace FinalAgent.Application.Exceptions;

public sealed class ConversationResponseException : Exception
{
    public ConversationResponseException(string message)
        : base(message)
    {
    }

    public ConversationResponseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
