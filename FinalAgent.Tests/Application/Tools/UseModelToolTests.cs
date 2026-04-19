using FinalAgent.Application.Models;
using FinalAgent.Application.Services;
using FinalAgent.Application.Tools;
using FinalAgent.Domain.Models;
using FluentAssertions;

namespace FinalAgent.Tests.Application.Tools;

public sealed class UseModelToolTests
{
    [Fact]
    public async Task ExecuteAsync_Should_ReturnInvalidArguments_When_ModelArgumentIsMissing()
    {
        UseModelTool sut = new(new ModelActivationService());
        ReplSessionContext session = new(
            new AgentProviderProfile(ProviderKind.OpenAiCompatible, "https://provider.example.com/v1"),
            "gpt-5-mini",
            ["gpt-5-mini"]);

        ToolResult result = await sut.ExecuteAsync(
            new ToolExecutionContext("call_1", "use_model", "{}", session),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.InvalidArguments);
        result.Message.Should().Be("Tool 'use_model' requires a 'model' or 'modelId' string argument.");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnInvalidArguments_When_ArgumentsJsonIsMalformed()
    {
        UseModelTool sut = new(new ModelActivationService());
        ReplSessionContext session = new(
            new AgentProviderProfile(ProviderKind.OpenAiCompatible, "https://provider.example.com/v1"),
            "gpt-5-mini",
            ["gpt-5-mini"]);

        ToolResult result = await sut.ExecuteAsync(
            new ToolExecutionContext("call_1", "use_model", "{", session),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.InvalidArguments);
        result.Message.Should().Be("Tool 'use_model' received invalid JSON arguments.");
    }

    [Fact]
    public async Task ExecuteAsync_Should_SwitchActiveModel_When_ModelIdCanBeResolved()
    {
        UseModelTool sut = new(new ModelActivationService());
        ReplSessionContext session = new(
            new AgentProviderProfile(ProviderKind.OpenAiCompatible, "https://provider.example.com/v1"),
            "qwen/qwen3-coder-30b",
            ["qwen/qwen3-coder-30b", "openai/gpt-oss-20b"]);

        ToolResult result = await sut.ExecuteAsync(
            new ToolExecutionContext(
                "call_1",
                "use_model",
                """{ "model": "gpt-oss-20b" }""",
                session),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.Success);
        result.Message.Should().Be("Active model switched to 'openai/gpt-oss-20b'.");
        session.ActiveModelId.Should().Be("openai/gpt-oss-20b");
    }
}
