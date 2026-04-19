using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Tools.Services;
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
                "show_config",
                ToolResult.Success("Config shown.")))
            .ReturnsAsync(new ToolInvocationResult(
                "call_2",
                "use_model",
                ToolResult.InvalidArguments("Model 'x' is not available.")));

        ToolExecutionPipeline sut = new(toolInvoker.Object);

        ToolExecutionBatchResult result = await sut.ExecuteAsync(
            [
                new ConversationToolCall("call_1", "show_config", "{}"),
                new ConversationToolCall("call_2", "use_model", """{ "model": "x" }""")
            ],
            Session,
            CancellationToken.None);

        result.Results.Select(item => item.ToolCallId).Should().Equal("call_1", "call_2");
        result.HasFailures.Should().BeTrue();
        result.ToDisplayText().Should().Contain("Tool 'show_config' completed.");
        result.ToDisplayText().Should().Contain("Tool 'use_model' rejected the provided arguments.");
    }
}
