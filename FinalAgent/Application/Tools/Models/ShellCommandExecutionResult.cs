namespace FinalAgent.Application.Tools.Models;

public sealed record ShellCommandExecutionResult(
    string Command,
    string WorkingDirectory,
    int ExitCode,
    string StandardOutput,
    string StandardError);
