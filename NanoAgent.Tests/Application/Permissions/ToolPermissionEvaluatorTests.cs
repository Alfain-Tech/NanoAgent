using System.Text.Json;
using NanoAgent.Application.Models;
using NanoAgent.Application.Permissions;
using NanoAgent.Domain.Models;
using FluentAssertions;

namespace NanoAgent.Tests.Application.Permissions;

public sealed class ToolPermissionEvaluatorTests : IDisposable
{
    private readonly string _workspaceRoot;

    public ToolPermissionEvaluatorTests()
    {
        _workspaceRoot = Path.Combine(
            Path.GetTempPath(),
            $"NanoAgent-Permissions-{Guid.NewGuid():N}");

        Directory.CreateDirectory(_workspaceRoot);
        Directory.CreateDirectory(Path.Combine(_workspaceRoot, "src"));
        Directory.CreateDirectory(Path.Combine(_workspaceRoot, "docs"));
    }

    [Fact]
    public void Evaluate_Should_Allow_When_PathIsWithinAllowedRoot()
    {
        ToolPermissionEvaluator sut = new(new StubWorkspaceRootProvider(_workspaceRoot));

        PermissionEvaluationResult result = sut.Evaluate(
            new ToolPermissionPolicy
            {
                FilePaths =
                [
                    new FilePathPermissionRule
                    {
                        ArgumentName = "path",
                        Kind = ToolPathAccessKind.Read,
                        AllowedRoots = ["src"]
                    }
                ]
            },
            new PermissionEvaluationContext(CreateContext("""{ "path": "src/app.cs" }""")));

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_Should_Deny_When_PathFallsOutsideAllowedRoot()
    {
        ToolPermissionEvaluator sut = new(new StubWorkspaceRootProvider(_workspaceRoot));

        PermissionEvaluationResult result = sut.Evaluate(
            new ToolPermissionPolicy
            {
                FilePaths =
                [
                    new FilePathPermissionRule
                    {
                        ArgumentName = "path",
                        Kind = ToolPathAccessKind.Write,
                        AllowedRoots = ["src"]
                    }
                ]
            },
            new PermissionEvaluationContext(CreateContext("""{ "path": "docs/readme.md" }""")));

        result.Decision.Should().Be(PermissionEvaluationDecision.Denied);
        result.ReasonCode.Should().Be("path_not_allowed");
    }

    [Fact]
    public void Evaluate_Should_ReturnRequiresApproval_When_PolicyRequiresApproval()
    {
        ToolPermissionEvaluator sut = new(new StubWorkspaceRootProvider(_workspaceRoot));

        PermissionEvaluationResult result = sut.Evaluate(
            new ToolPermissionPolicy
            {
                ApprovalMode = ToolApprovalMode.RequireApproval
            },
            new PermissionEvaluationContext(CreateContext("{}")));

        result.Decision.Should().Be(PermissionEvaluationDecision.RequiresApproval);
        result.ReasonCode.Should().Be("approval_required");
    }

    [Fact]
    public void Evaluate_Should_Deny_When_ShellCommandIsNotAllowlisted()
    {
        ToolPermissionEvaluator sut = new(new StubWorkspaceRootProvider(_workspaceRoot));

        PermissionEvaluationResult result = sut.Evaluate(
            new ToolPermissionPolicy
            {
                Shell = new ShellCommandPermissionPolicy
                {
                    CommandArgumentName = "command",
                    AllowedCommands = ["git", "dotnet"]
                }
            },
            new PermissionEvaluationContext(CreateContext("""{ "command": "rm -rf ." }""")));

        result.Decision.Should().Be(PermissionEvaluationDecision.Denied);
        result.ReasonCode.Should().Be("shell_command_not_allowed");
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspaceRoot))
        {
            Directory.Delete(_workspaceRoot, recursive: true);
        }
    }

    private static ToolExecutionContext CreateContext(string argumentsJson)
    {
        using JsonDocument document = JsonDocument.Parse(argumentsJson);

        return new ToolExecutionContext(
            "call_1",
            "tool",
            document.RootElement.Clone(),
            new ReplSessionContext(
                new AgentProviderProfile(ProviderKind.OpenAi, null),
                "gpt-5-mini",
                ["gpt-5-mini"]));
    }

    private sealed class StubWorkspaceRootProvider : global::NanoAgent.Application.Abstractions.IWorkspaceRootProvider
    {
        private readonly string _workspaceRoot;

        public StubWorkspaceRootProvider(string workspaceRoot)
        {
            _workspaceRoot = workspaceRoot;
        }

        public string GetWorkspaceRoot()
        {
            return _workspaceRoot;
        }
    }
}
