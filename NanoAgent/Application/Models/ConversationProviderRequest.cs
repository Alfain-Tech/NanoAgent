using NanoAgent.Domain.Models;

namespace NanoAgent.Application.Models;

public sealed record ConversationProviderRequest(
    AgentProviderProfile ProviderProfile,
    string ApiKey,
    string ModelId,
    string UserInput,
    string? SystemPrompt,
    IReadOnlyList<ToolDefinition> AvailableTools);
