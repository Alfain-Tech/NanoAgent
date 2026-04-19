using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Tools;
using FinalAgent.Application.Tools.Models;
using FluentAssertions;
using Moq;

namespace FinalAgent.Tests.Application.Tools;

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
                    request.WorkingDirectory == "FinalAgent"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShellCommandExecutionResult(
                "dotnet --version",
                "FinalAgent",
                0,
                "10.0.103",
                string.Empty));

        ShellCommandTool sut = new(shellCommandService.Object);

        ToolResult result = await sut.ExecuteAsync(
            CreateContext("""{ "command": "dotnet --version", "workingDirectory": "FinalAgent" }"""),
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
