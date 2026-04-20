using System.Text.Json;
using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;
using NanoAgent.Application.Tools;
using NanoAgent.Application.Tools.Models;
using FluentAssertions;
using Moq;

namespace NanoAgent.Tests.Application.Tools;

public sealed class ApplyPatchToolTests
{
    [Fact]
    public async Task ExecuteAsync_Should_ReturnInvalidArguments_When_PatchIsMissing()
    {
        ApplyPatchTool sut = new(Mock.Of<IWorkspaceFileService>());

        ToolResult result = await sut.ExecuteAsync(
            CreateContext("{}"),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.InvalidArguments);
        result.Message.Should().Contain("requires a non-empty 'patch'");
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnStructuredResult_When_PatchApplies()
    {
        Mock<IWorkspaceFileService> workspaceFileService = new(MockBehavior.Strict);
        workspaceFileService
            .Setup(service => service.ApplyPatchAsync(
                "*** Begin Patch\n*** End Patch",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceApplyPatchResult(
                1,
                1,
                0,
                [
                    new WorkspaceApplyPatchFileResult(
                        "README.md",
                        "update",
                        null,
                        1,
                        0,
                        [new WorkspaceFileWritePreviewLine(1, "add", "hello")],
                        0)
                ]));

        ApplyPatchTool sut = new(workspaceFileService.Object);

        ToolResult result = await sut.ExecuteAsync(
            CreateContext("""{ "patch": "*** Begin Patch\n*** End Patch" }"""),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.Success);
        result.JsonResult.Should().Contain("\"FileCount\":1");
        result.RenderPayload!.Text.Should().Contain("README.md");
    }

    private static ToolExecutionContext CreateContext(string argumentsJson)
    {
        using JsonDocument document = JsonDocument.Parse(argumentsJson);
        return new ToolExecutionContext(
            "call_1",
            "apply_patch",
            document.RootElement.Clone(),
            TestSessionFactory.Create());
    }
}
