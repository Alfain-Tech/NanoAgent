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
            new StubTool("show_config"),
            new StubTool("use_model")
        ]);

        bool found = sut.TryResolve("use_model", out IAgentTool? tool);

        found.Should().BeTrue();
        tool.Should().NotBeNull();
        tool!.Name.Should().Be("use_model");
        sut.GetRegisteredToolNames().Should().Equal("show_config", "use_model");
    }

    [Fact]
    public void Constructor_Should_Throw_When_DuplicateToolNamesAreRegistered()
    {
        Action action = () => new ToolRegistry([
            new StubTool("show_config"),
            new StubTool("show_config")
        ]);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate tool registration*");
    }

    private sealed class StubTool : IAgentTool
    {
        public StubTool(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public Task<ToolResult> ExecuteAsync(
            ToolExecutionContext context,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
