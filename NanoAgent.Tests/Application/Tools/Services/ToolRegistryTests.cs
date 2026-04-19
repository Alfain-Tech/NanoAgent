using System.Text.Json;
using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;
using NanoAgent.Application.Permissions;
using NanoAgent.Application.Tools.Services;
using FluentAssertions;

namespace NanoAgent.Tests.Application.Tools.Services;

public sealed class ToolRegistryTests
{
    [Fact]
    public void TryResolve_Should_ReturnRegisteredTool_When_NameExists()
    {
        ToolRegistry sut = new([
            new StubTool("directory_list"),
            new StubTool("file_read")
        ], new ToolPermissionParser());

        bool found = sut.TryResolve("file_read", out ToolRegistration? tool);

        found.Should().BeTrue();
        tool.Should().NotBeNull();
        tool!.Name.Should().Be("file_read");
        tool.PermissionPolicy.FilePaths.Should().ContainSingle();
        sut.GetRegisteredToolNames().Should().Equal("directory_list", "file_read");
        sut.GetToolDefinitions().Select(definition => definition.Name).Should().Equal("directory_list", "file_read");
    }

    [Fact]
    public void Constructor_Should_Throw_When_DuplicateToolNamesAreRegistered()
    {
        Action action = () => new ToolRegistry([
            new StubTool("directory_list"),
            new StubTool("directory_list")
        ], new ToolPermissionParser());

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate tool registration*");
    }

    private sealed class StubTool : ITool
    {
        public StubTool(string name)
        {
            Name = name;
        }

        public string Description => $"Description for {Name}";

        public string Name { get; }

        public string PermissionRequirements => """
            {
              "approvalMode": "Automatic",
              "filePaths": [
                {
                  "argumentName": "path",
                  "kind": "Read",
                  "allowedRoots": ["src"]
                }
              ]
            }
            """;

        public string Schema => """{ "type": "object", "properties": {}, "additionalProperties": false }""";

        public Task<ToolResult> ExecuteAsync(
            ToolExecutionContext context,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
