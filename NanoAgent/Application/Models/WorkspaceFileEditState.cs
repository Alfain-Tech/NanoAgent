namespace NanoAgent.Application.Models;

public sealed class WorkspaceFileEditState
{
    public WorkspaceFileEditState(
        string path,
        bool exists,
        string? content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (exists && content is null)
        {
            throw new ArgumentException(
                "Existing file states must include content.",
                nameof(content));
        }

        Path = path.Trim();
        Exists = exists;
        Content = content;
    }

    public string? Content { get; }

    public bool Exists { get; }

    public string Path { get; }
}
