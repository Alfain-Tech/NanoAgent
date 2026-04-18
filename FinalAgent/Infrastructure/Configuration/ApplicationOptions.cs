namespace FinalAgent.Infrastructure.Configuration;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    public string ProductName { get; set; } = string.Empty;

    public string StorageDirectoryName { get; set; } = string.Empty;
}
