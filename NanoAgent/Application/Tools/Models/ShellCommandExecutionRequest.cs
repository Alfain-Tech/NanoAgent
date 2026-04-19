namespace NanoAgent.Application.Tools.Models;

public sealed record ShellCommandExecutionRequest(
    string Command,
    string? WorkingDirectory);
