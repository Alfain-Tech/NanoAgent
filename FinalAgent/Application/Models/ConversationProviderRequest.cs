using FinalAgent.Domain.Models;

namespace FinalAgent.Application.Models;

public sealed record ConversationProviderRequest(
    AgentProviderProfile ProviderProfile,
    string ApiKey,
    string ModelId,
    string UserInput,
    string? SystemPrompt);
