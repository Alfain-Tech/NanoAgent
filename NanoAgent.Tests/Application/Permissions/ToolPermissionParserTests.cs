using NanoAgent.Application.Models;
using NanoAgent.Application.Permissions;
using FluentAssertions;

namespace NanoAgent.Tests.Application.Permissions;

public sealed class ToolPermissionParserTests
{
    [Fact]
    public void Parse_Should_ReturnNormalizedPolicy_When_JsonIsValid()
    {
        ToolPermissionParser sut = new();

        ToolPermissionPolicy result = sut.Parse(
            "file_read",
            """
            {
              "approvalMode": "Automatic",
              "filePaths": [
                {
                  "argumentName": "path",
                  "kind": "Read",
                  "allowedRoots": [" src ", "src"]
                }
              ],
              "shell": {
                "commandArgumentName": " command ",
                "allowedCommands": [" git ", "git", "dotnet"]
              }
            }
            """);

        result.ApprovalMode.Should().Be(ToolApprovalMode.Automatic);
        result.FilePaths.Should().ContainSingle();
        result.FilePaths[0].AllowedRoots.Should().Equal("src");
        result.Shell.Should().NotBeNull();
        result.Shell!.CommandArgumentName.Should().Be("command");
        result.Shell.AllowedCommands.Should().Equal("git", "dotnet");
    }

    [Fact]
    public void Parse_Should_Throw_When_ShellAllowlistIsEmpty()
    {
        ToolPermissionParser sut = new();

        Action action = () => sut.Parse(
            "shell_command",
            """
            {
              "approvalMode": "Automatic",
              "shell": {
                "commandArgumentName": "command",
                "allowedCommands": []
              }
            }
            """);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one allowed shell command*");
    }
}
