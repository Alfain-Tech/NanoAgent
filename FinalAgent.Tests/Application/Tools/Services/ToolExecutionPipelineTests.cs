using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Tools.Services;
using FinalAgent.Application.Tools.Serialization;
using FinalAgent.Domain.Models;
using FluentAssertions;
using Moq;

namespace FinalAgent.Tests.Application.Tools.Services;

public sealed class ToolExecutionPipelineTests
{
    private static readonly ReplSessionContext Session = new(
        new AgentProviderProfile(ProviderKind.OpenAiCompatible, "https://provider.example.com/v1"),
        "gpt-5-mini",
        ["gpt-5-mini", "gpt-4.1"]);

    [Fact]
    public async Task ExecuteAsync_Should_ReturnStructuredResultsInInputOrder()
    {
        Mock<IToolInvoker> toolInvoker = new(MockBehavior.Strict);
        toolInvoker
            .SetupSequence(invoker => invoker.InvokeAsync(
                It.IsAny<ConversationToolCall>(),
                Session,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolInvocationResult(
                "call_1",
                "file_read",
                ToolResultFactory.Success(
                    "Read file 'README.md'.",
                    new ToolErrorPayload("info", "ok"),
                    ToolJsonContext.Default.ToolErrorPayload,
                    new ToolRenderPayload("File: README.md", "hello"))))
            .ReturnsAsync(new ToolInvocationResult(
                "call_2",
                "shell_command",
                ToolResultFactory.InvalidArguments(
                    "invalid_command",
                    "Tool 'shell_command' requires a non-empty 'command' string.",
                    new ToolRenderPayload("Invalid shell_command arguments", "Provide a non-empty command."))));

        ToolExecutionPipeline sut = new(toolInvoker.Object);

        ToolExecutionBatchResult result = await sut.ExecuteAsync(
            [
                new ConversationToolCall("call_1", "file_read", """{ "path": "README.md" }"""),
                new ConversationToolCall("call_2", "shell_command", "{}")
            ],
            Session,
            CancellationToken.None);

        result.Results.Select(item => item.ToolCallId).Should().Equal("call_1", "call_2");
        result.HasFailures.Should().BeTrue();
        result.ToDisplayText().Should().Contain("File: README.md");
        result.ToDisplayText().Should().Contain("Invalid shell_command arguments");
    }
}
