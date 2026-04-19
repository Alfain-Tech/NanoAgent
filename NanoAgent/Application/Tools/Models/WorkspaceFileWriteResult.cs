namespace NanoAgent.Application.Tools.Models;

public sealed record WorkspaceFileWriteResult(
    string Path,
    bool OverwroteExistingFile,
    int CharacterCount);
