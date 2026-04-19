using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IConversationResponseMapper
{
    ConversationResponse Map(ConversationProviderPayload payload);
}
