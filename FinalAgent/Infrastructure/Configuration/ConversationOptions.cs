namespace FinalAgent.Infrastructure.Configuration;

public sealed class ConversationOptions
{
    public int RequestTimeoutSeconds { get; set; } = 30;

    public string? SystemPrompt { get; set; } =
        "You are FinalAgent, a precise CLI coding assistant. Keep answers concise, actionable, and tool-aware.";
}
