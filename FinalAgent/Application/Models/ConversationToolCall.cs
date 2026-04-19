namespace FinalAgent.Application.Models;

public sealed record ConversationToolCall(
    string Id,
    string Name,
    string ArgumentsJson);
