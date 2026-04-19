namespace FinalAgent.Application.Tools.Models;

public sealed record WorkspaceFileReadResult(
    string Path,
    string Content,
    int CharacterCount);
