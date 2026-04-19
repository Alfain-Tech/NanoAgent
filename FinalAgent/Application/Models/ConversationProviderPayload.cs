using FinalAgent.Domain.Models;

namespace FinalAgent.Application.Models;

public sealed record ConversationProviderPayload(
    ProviderKind ProviderKind,
    string RawContent,
    string? ResponseId);
