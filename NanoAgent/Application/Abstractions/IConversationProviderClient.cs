using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IConversationProviderClient
{
    Task<ConversationProviderPayload> SendAsync(
        ConversationProviderRequest request,
        CancellationToken cancellationToken);
}
