using System.Text.Json;
using NanoAgent.Application.Models;
using NanoAgent.Application.Tools;
using FluentAssertions;

namespace NanoAgent.Tests.Application.Tools;

public sealed class PlanningModeToolTests
{
    [Fact]
    public async Task ExecuteAsync_Should_ReturnInvalidArguments_When_ObjectiveIsMissing()
    {
        PlanningModeTool sut = new();

        ToolResult result = await sut.ExecuteAsync(
            CreateContext("{}"),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.InvalidArguments);
        result.Message.Should().Contain("requires a non-empty 'objective'");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnStructuredResult_When_ObjectiveIsProvided()
    {
        PlanningModeTool sut = new();

        ToolResult result = await sut.ExecuteAsync(
            CreateContext("""{ "objective": "Refactor the pipeline." }"""),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.Success);
        result.Message.Should().Contain("Planning mode activated");
        result.JsonResult.Should().Contain("\"Objective\":\"Refactor the pipeline.\"");
        result.JsonResult.Should().Contain("\"SuggestedResponseSections\"");
        result.JsonResult.Should().Contain("\"Environment / toolchain\"");
        result.JsonResult.Should().Contain("\"Toolchain commands\"");
        result.JsonResult.Should().Contain("\"Validation\"");
        result.JsonResult.Should().Contain("\"Recommended approach\"");
        result.RenderPayload.Should().NotBeNull();
        result.RenderPayload!.Title.Should().Contain("Planning mode");
        result.RenderPayload.Text.Should().Contain("Check installed build tools, compilers, SDKs");
        result.RenderPayload.Text.Should().Contain("scaffold, build, or test commands");
        result.RenderPayload.Text.Should().Contain("project scaffolding, dependency restore/install");
        result.RenderPayload.Text.Should().Contain("Separate verified findings from assumptions");
        result.RenderPayload.Text.Should().Contain("high-quality ordered task list");
        result.RenderPayload.Text.Should().Contain("one active step at a time");
    }

    private static ToolExecutionContext CreateContext(string argumentsJson)
    {
        using JsonDocument document = JsonDocument.Parse(argumentsJson);
        return new ToolExecutionContext(
            "call_1",
            AgentToolNames.PlanningMode,
            document.RootElement.Clone(),
            TestSessionFactory.Create());
    }
}
