using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IConversationResponseMapper
{
    ConversationResponse Map(ConversationProviderPayload payload);
}
