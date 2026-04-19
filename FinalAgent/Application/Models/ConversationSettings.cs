namespace FinalAgent.Application.Models;

public sealed record ConversationSettings(
    string? SystemPrompt,
    TimeSpan RequestTimeout);
