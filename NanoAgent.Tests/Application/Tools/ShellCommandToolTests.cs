using System.Text.Json;
using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;
using NanoAgent.Application.Tools;
using NanoAgent.Application.Tools.Models;
using FluentAssertions;
using Moq;

namespace NanoAgent.Tests.Application.Tools;

public sealed class ShellCommandToolTests
{
    [Fact]
    public async Task ExecuteAsync_Should_ReturnInvalidArguments_When_CommandIsMissing()
    {
        ShellCommandTool sut = new(Mock.Of<IShellCommandService>());

        ToolResult result = await sut.ExecuteAsync(
            CreateContext("{}"),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.InvalidArguments);
        result.Message.Should().Contain("requires a non-empty 'command'");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnStructuredShellResult_When_CommandRuns()
    {
        Mock<IShellCommandService> shellCommandService = new(MockBehavior.Strict);
        shellCommandService
            .Setup(service => service.ExecuteAsync(
                It.Is<ShellCommandExecutionRequest>(request =>
                    request.Command == "dotnet --version" &&
                    request.WorkingDirectory == "NanoAgent"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShellCommandExecutionResult(
                "dotnet --version",
                "NanoAgent",
                0,
                "10.0.103",
                string.Empty));

        ShellCommandTool sut = new(shellCommandService.Object);

        ToolResult result = await sut.ExecuteAsync(
            CreateContext("""{ "command": "dotnet --version", "workingDirectory": "NanoAgent" }"""),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.Success);
        result.JsonResult.Should().Contain("\"ExitCode\":0");
        result.RenderPayload!.Text.Should().Contain("10.0.103");
    }

    private static ToolExecutionContext CreateContext(string argumentsJson)
    {
        using JsonDocument document = JsonDocument.Parse(argumentsJson);
        return new ToolExecutionContext(
            "call_1",
            "shell_command",
            document.RootElement.Clone(),
            TestSessionFactory.Create());
    }
}
