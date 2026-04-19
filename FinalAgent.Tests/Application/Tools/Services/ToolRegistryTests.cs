using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Tools.Services;
using FluentAssertions;

namespace FinalAgent.Tests.Application.Tools.Services;

public sealed class ToolRegistryTests
{
    [Fact]
    public void TryResolve_Should_ReturnRegisteredTool_When_NameExists()
    {
        ToolRegistry sut = new([
            new StubTool("directory_list"),
            new StubTool("file_read")
        ]);

        bool found = sut.TryResolve("file_read", out ITool? tool);

        found.Should().BeTrue();
        tool.Should().NotBeNull();
        tool!.Name.Should().Be("file_read");
        sut.GetRegisteredToolNames().Should().Equal("directory_list", "file_read");
        sut.GetToolDefinitions().Select(definition => definition.Name).Should().Equal("directory_list", "file_read");
    }

    [Fact]
    public void Constructor_Should_Throw_When_DuplicateToolNamesAreRegistered()
    {
        Action action = () => new ToolRegistry([
            new StubTool("directory_list"),
            new StubTool("directory_list")
        ]);

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

        public string Schema => """{ "type": "object", "properties": {}, "additionalProperties": false }""";

        public Task<ToolResult> ExecuteAsync(
            ToolExecutionContext context,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
