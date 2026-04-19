using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Permissions;
using FinalAgent.Application.Tools.Services;
using FinalAgent.Application.Tools.Serialization;
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
        RegistryBackedToolInvoker sut = new(
            new ToolRegistry([], new ToolPermissionParser()),
            new ToolPermissionEvaluator(new StubWorkspaceRootProvider()));

        ToolInvocationResult result = await sut.InvokeAsync(
            new ConversationToolCall("call_1", "missing_tool", "{}"),
            Session,
            CancellationToken.None);

        result.Result.Status.Should().Be(ToolResultStatus.NotFound);
        result.Result.Message.Should().Contain("not registered");
        result.Result.JsonResult.Should().Contain("tool_not_found");
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnInvalidArguments_When_ToolArgumentsAreNotJsonObject()
    {
        RegistryBackedToolInvoker sut = new(new ToolRegistry([
            new EchoTool()
        ], new ToolPermissionParser()), new ToolPermissionEvaluator(new StubWorkspaceRootProvider()));

        ToolInvocationResult result = await sut.InvokeAsync(
            new ConversationToolCall("call_1", "echo_tool", "[]"),
            Session,
            CancellationToken.None);

        result.Result.Status.Should().Be(ToolResultStatus.InvalidArguments);
        result.Result.Message.Should().Contain("JSON-object arguments");
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnExecutionErrorResult_When_ToolThrowsUnexpectedly()
    {
        RegistryBackedToolInvoker sut = new(new ToolRegistry([
            new ThrowingTool()
        ], new ToolPermissionParser()), new ToolPermissionEvaluator(new StubWorkspaceRootProvider()));

        ToolInvocationResult result = await sut.InvokeAsync(
            new ConversationToolCall("call_1", "exploding_tool", "{}"),
            Session,
            CancellationToken.None);

        result.Result.Status.Should().Be(ToolResultStatus.ExecutionError);
        result.Result.Message.Should().Contain("boom");
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnExecutionError_When_ToolTimesOut()
    {
        RegistryBackedToolInvoker sut = new(
            new ToolRegistry([new SlowTool()], new ToolPermissionParser()),
            new ToolPermissionEvaluator(new StubWorkspaceRootProvider()),
            TimeSpan.FromMilliseconds(50));

        ToolInvocationResult result = await sut.InvokeAsync(
            new ConversationToolCall("call_1", "slow_tool", "{}"),
            Session,
            CancellationToken.None);

        result.Result.Status.Should().Be(ToolResultStatus.ExecutionError);
        result.Result.Message.Should().Contain("timed out");
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnPermissionDenied_When_ToolRequiresApproval()
    {
        ApprovalTool tool = new();
        RegistryBackedToolInvoker sut = new(
            new ToolRegistry([tool], new ToolPermissionParser()),
            new ToolPermissionEvaluator(new StubWorkspaceRootProvider()));

        ToolInvocationResult result = await sut.InvokeAsync(
            new ConversationToolCall("call_1", "approval_tool", """{ "path": "src/app.cs" }"""),
            Session,
            CancellationToken.None);

        result.Result.Status.Should().Be(ToolResultStatus.PermissionDenied);
        result.Result.Message.Should().Contain("requires explicit approval");
        tool.WasExecuted.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnPermissionDenied_When_ShellCommandIsNotAllowed()
    {
        RegistryBackedToolInvoker sut = new(
            new ToolRegistry([new ShellRestrictedTool()], new ToolPermissionParser()),
            new ToolPermissionEvaluator(new StubWorkspaceRootProvider()));

        ToolInvocationResult result = await sut.InvokeAsync(
            new ConversationToolCall("call_1", "shell_restricted_tool", """{ "command": "rm -rf ." }"""),
            Session,
            CancellationToken.None);

        result.Result.Status.Should().Be(ToolResultStatus.PermissionDenied);
        result.Result.Message.Should().Contain("Allowed commands");
    }

    private sealed class EchoTool : ITool
    {
        public string Description => "Echo tool";

        public string Name => "echo_tool";

        public string PermissionRequirements => """{ "approvalMode": "Automatic" }""";

        public string Schema => """{ "type": "object", "properties": {}, "additionalProperties": false }""";

        public Task<ToolResult> ExecuteAsync(
            ToolExecutionContext context,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ToolResultFactory.Success(
                "Echoed.",
                new ToolErrorPayload("echo", "ok"),
                ToolJsonContext.Default.ToolErrorPayload));
        }
    }

    private sealed class SlowTool : ITool
    {
        public string Description => "Slow tool";

        public string Name => "slow_tool";

        public string PermissionRequirements => """{ "approvalMode": "Automatic" }""";

        public string Schema => """{ "type": "object", "properties": {}, "additionalProperties": false }""";

        public async Task<ToolResult> ExecuteAsync(
            ToolExecutionContext context,
            CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return ToolResultFactory.Success(
                "Completed.",
                new ToolErrorPayload("slow", "done"),
                ToolJsonContext.Default.ToolErrorPayload);
        }
    }

    private sealed class ThrowingTool : ITool
    {
        public string Description => "Throwing tool";

        public string Name => "exploding_tool";

        public string PermissionRequirements => """{ "approvalMode": "Automatic" }""";

        public string Schema => """{ "type": "object", "properties": {}, "additionalProperties": false }""";

        public Task<ToolResult> ExecuteAsync(
            ToolExecutionContext context,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("boom");
        }
    }

    private sealed class ApprovalTool : ITool
    {
        public bool WasExecuted { get; private set; }

        public string Description => "Approval tool";

        public string Name => "approval_tool";

        public string PermissionRequirements => """
            {
              "approvalMode": "RequireApproval",
              "filePaths": [
                {
                  "argumentName": "path",
                  "kind": "Read",
                  "allowedRoots": ["src"]
                }
              ]
            }
            """;

        public string Schema => """{ "type": "object", "properties": { "path": { "type": "string" } }, "additionalProperties": false }""";

        public Task<ToolResult> ExecuteAsync(
            ToolExecutionContext context,
            CancellationToken cancellationToken)
        {
            WasExecuted = true;
            return Task.FromResult(ToolResultFactory.Success(
                "Executed.",
                new ToolErrorPayload("ok", "ok"),
                ToolJsonContext.Default.ToolErrorPayload));
        }
    }

    private sealed class ShellRestrictedTool : ITool
    {
        public string Description => "Shell restricted tool";

        public string Name => "shell_restricted_tool";

        public string PermissionRequirements => """
            {
              "approvalMode": "Automatic",
              "shell": {
                "commandArgumentName": "command",
                "allowedCommands": ["git", "dotnet"]
              }
            }
            """;

        public string Schema => """{ "type": "object", "properties": { "command": { "type": "string" } }, "additionalProperties": false }""";

        public Task<ToolResult> ExecuteAsync(
            ToolExecutionContext context,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubWorkspaceRootProvider : IWorkspaceRootProvider
    {
        public string GetWorkspaceRoot()
        {
            return Path.GetTempPath();
        }
    }
}
