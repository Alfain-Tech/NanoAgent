using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Tools;
using FinalAgent.Application.Tools.Models;
using FluentAssertions;
using Moq;

namespace FinalAgent.Tests.Application.Tools;

public sealed class DirectoryListToolTests
{
    [Fact]
    public async Task ExecuteAsync_Should_ListDirectoryContents()
    {
        Mock<IWorkspaceFileService> workspaceFileService = new(MockBehavior.Strict);
        workspaceFileService
            .Setup(service => service.ListDirectoryAsync(
                "src",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceDirectoryListResult(
                "src",
                [new WorkspaceDirectoryEntry("src/Program.cs", "file")]));

        DirectoryListTool sut = new(workspaceFileService.Object);

        ToolResult result = await sut.ExecuteAsync(
            CreateContext("""{ "path": "src", "recursive": true }"""),
            CancellationToken.None);

        result.Status.Should().Be(ToolResultStatus.Success);
        result.JsonResult.Should().Contain("Program.cs");
        result.RenderPayload!.Text.Should().Contain("file: src/Program.cs");
    }

    private static ToolExecutionContext CreateContext(string argumentsJson)
    {
        using JsonDocument document = JsonDocument.Parse(argumentsJson);
        return new ToolExecutionContext(
            "call_1",
            "directory_list",
            document.RootElement.Clone(),
            TestSessionFactory.Create());
    }
}
