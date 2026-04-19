using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Tools.Services;
using FinalAgent.Domain.Models;
using FluentAssertions;

namespace FinalAgent.Tests.Application.Tools.Services;

public sealed class RegistryBackedToolInvokerTests
{
    private static readonly ReplSessionContext Session = new(
        new AgentProviderProfile(ProviderKind.OpenAi, null),
        "gpt-5-mini",
        ["gpt-5-mini"]);

    [Fact]
    public async Task InvokeAsync_Should_ReturnNotFoundResult_When_ToolIsUnknown()
    {
        RegistryBackedToolInvoker sut = new(new ToolRegistry([]));

        ToolInvocationResult result = await sut.InvokeAsync(
            new ConversationToolCall("call_1", "missing_tool", "{}"),
            Session,
            CancellationToken.None);

        result.Result.Status.Should().Be(ToolResultStatus.NotFound);
        result.Result.Message.Should().Contain("not registered");
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnExecutionErrorResult_When_ToolThrowsUnexpectedly()
    {
        RegistryBackedToolInvoker sut = new(new ToolRegistry([
            new ThrowingTool()
        ]));

        ToolInvocationResult result = await sut.InvokeAsync(
            new ConversationToolCall("call_1", "exploding_tool", "{}"),
            Session,
            CancellationToken.None);

        result.Result.Status.Should().Be(ToolResultStatus.ExecutionError);
        result.Result.Message.Should().Contain("boom");
    }

    private sealed class ThrowingTool : IAgentTool
    {
        public string Name => "exploding_tool";

        public Task<ToolResult> ExecuteAsync(
            ToolExecutionContext context,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
