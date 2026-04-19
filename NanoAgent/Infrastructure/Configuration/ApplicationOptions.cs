namespace NanoAgent.Infrastructure.Configuration;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    public string ProductName { get; set; } = string.Empty;

    public string StorageDirectoryName { get; set; } = string.Empty;

    public ConversationOptions Conversation { get; set; } = new();

    public ApplicationDefaultsOptions Defaults { get; set; } = new();

    public ModelSelectionOptions ModelSelection { get; set; } = new();
}
