namespace NanoAgent.Application.Exceptions;

public sealed class ConversationPipelineException : Exception
{
    public ConversationPipelineException(string message)
        : base(message)
    {
    }

    public ConversationPipelineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
