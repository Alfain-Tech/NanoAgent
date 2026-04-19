namespace FinalAgent.Application.Models;

public sealed class ShellCommandPermissionPolicy
{
    public string[] AllowedCommands { get; set; } = [];

    public string CommandArgumentName { get; set; } = "command";
}
