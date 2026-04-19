using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IConversationProviderClient
{
    Task<ConversationProviderPayload> SendAsync(
        ConversationProviderRequest request,
        CancellationToken cancellationToken);
}
