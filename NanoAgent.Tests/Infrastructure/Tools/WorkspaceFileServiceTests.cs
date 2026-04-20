using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Tools.Models;
using NanoAgent.Infrastructure.Secrets;
using NanoAgent.Infrastructure.Tools;
using NanoAgent.Tests.Infrastructure.Secrets.TestDoubles;
using FluentAssertions;

namespace NanoAgent.Tests.Infrastructure.Tools;

public sealed class WorkspaceFileServiceTests : IDisposable
{
    private readonly FakeProcessRunner _processRunner = new();
    private readonly string _workspaceRoot;

    public WorkspaceFileServiceTests()
    {
        _workspaceRoot = Path.Combine(
            Path.GetTempPath(),
            $"NanoAgent-Workspace-{Guid.NewGuid():N}");

        Directory.CreateDirectory(_workspaceRoot);
    }

    [Fact]
    public async Task WriteFileAsync_Should_CreateAddedLinePreview_When_FileIsNew()
    {
        WorkspaceFileService sut = CreateSut();

        WorkspaceFileWriteResult result = await sut.WriteFileAsync(
            "index.html",
            "<!DOCTYPE html>\n<html lang=\"en\">\n<body>\n</body>",
            overwrite: true,
            CancellationToken.None);

        result.AddedLineCount.Should().Be(4);
        result.RemovedLineCount.Should().Be(0);
        result.PreviewLines.Should().ContainInOrder(
            new WorkspaceFileWritePreviewLine(1, "add", "<!DOCTYPE html>"),
            new WorkspaceFileWritePreviewLine(2, "add", "<html lang=\"en\">"),
            new WorkspaceFileWritePreviewLine(3, "add", "<body>"),
            new WorkspaceFileWritePreviewLine(4, "add", "</body>"));
    }

    [Fact]
    public async Task WriteFileAsync_Should_CreateContextAwarePreview_When_FileIsUpdated()
    {
        WorkspaceFileService sut = CreateSut();
        string filePath = Path.Combine(_workspaceRoot, "styles.css");

        await File.WriteAllTextAsync(
            filePath,
            ".card {\n  color: red;\n}\n",
            CancellationToken.None);

        WorkspaceFileWriteResult result = await sut.WriteFileAsync(
            "styles.css",
            ".card {\n  color: blue;\n}\n",
            overwrite: true,
            CancellationToken.None);

        result.AddedLineCount.Should().Be(1);
        result.RemovedLineCount.Should().Be(1);
        result.PreviewLines.Should().ContainInOrder(
            new WorkspaceFileWritePreviewLine(1, "context", ".card {"),
            new WorkspaceFileWritePreviewLine(2, "remove", "  color: red;"),
            new WorkspaceFileWritePreviewLine(2, "add", "  color: blue;"),
            new WorkspaceFileWritePreviewLine(3, "context", "}"));
    }

    [Fact]
    public async Task ReadFileAsync_Should_UseShellBackedRead_When_CommandSucceeds()
    {
        WorkspaceFileService sut = CreateSut();
        string filePath = Path.Combine(_workspaceRoot, "README.md");

        await File.WriteAllTextAsync(filePath, "disk", CancellationToken.None);
        _processRunner.EnqueueResult(new ProcessExecutionResult(0, "shell", string.Empty));

        WorkspaceFileReadResult result = await sut.ReadFileAsync(
            "README.md",
            CancellationToken.None);

        result.Content.Should().Be("shell");
        _processRunner.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchFilesAsync_Should_ReturnShellMatches_AsWorkspaceRelativePaths()
    {
        WorkspaceFileService sut = CreateSut();
        string srcDirectory = Path.Combine(_workspaceRoot, "src");
        Directory.CreateDirectory(srcDirectory);
        string programPath = Path.Combine(srcDirectory, "Program.cs");
        await File.WriteAllTextAsync(programPath, "class Program {}", CancellationToken.None);

        _processRunner.EnqueueResult(new ProcessExecutionResult(
            0,
            programPath,
            string.Empty));

        WorkspaceFileSearchResult result = await sut.SearchFilesAsync(
            new WorkspaceFileSearchRequest("Program", "src", CaseSensitive: false),
            CancellationToken.None);

        result.Matches.Should().Equal("src/Program.cs");
        _processRunner.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchTextAsync_Should_NormalizeShellMatches_ToWorkspaceRelativePaths()
    {
        WorkspaceFileService sut = CreateSut();
        string srcDirectory = Path.Combine(_workspaceRoot, "src");
        Directory.CreateDirectory(srcDirectory);
        string programPath = Path.Combine(srcDirectory, "Program.cs");
        await File.WriteAllTextAsync(programPath, "class Program {}", CancellationToken.None);

        _processRunner.EnqueueResult(CreateSearchTextResult(programPath));

        WorkspaceTextSearchResult result = await sut.SearchTextAsync(
            new WorkspaceTextSearchRequest("Program", "src", CaseSensitive: false),
            CancellationToken.None);

        result.Matches.Should().ContainSingle();
        result.Matches[0].Path.Should().Be("src/Program.cs");
        result.Matches[0].LineNumber.Should().Be(1);
        result.Matches[0].LineText.Should().Contain("Program");
    }

    [Fact]
    public async Task ApplyPatchAsync_Should_UpdateAndAddFiles()
    {
        WorkspaceFileService sut = CreateSut();
        string existingFile = Path.Combine(_workspaceRoot, "src", "Program.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(existingFile)!);
        await File.WriteAllTextAsync(
            existingFile,
            "class Program\n{\n    // TODO\n}\n",
            CancellationToken.None);

        WorkspaceApplyPatchResult result = await sut.ApplyPatchAsync(
            """
            *** Begin Patch
            *** Update File: src/Program.cs
            @@
             class Program
             {
            -    // TODO
            +    // done
             }
            *** Add File: src/Notes.txt
            +remember the tests
            *** End Patch
            """,
            CancellationToken.None);

        result.FileCount.Should().Be(2);
        result.Files.Select(static file => file.Path).Should().BeEquivalentTo(["src/Program.cs", "src/Notes.txt"]);
        (await File.ReadAllTextAsync(existingFile, CancellationToken.None)).Should().Contain("// done");
        (await File.ReadAllTextAsync(Path.Combine(_workspaceRoot, "src", "Notes.txt"), CancellationToken.None))
            .Should().Be("remember the tests");
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspaceRoot))
        {
            Directory.Delete(_workspaceRoot, recursive: true);
        }
    }

    private WorkspaceFileService CreateSut()
    {
        return new WorkspaceFileService(
            new StubWorkspaceRootProvider(_workspaceRoot),
            _processRunner);
    }

    private static ProcessExecutionResult CreateSearchTextResult(string absolutePath)
    {
        if (OperatingSystem.IsWindows())
        {
            string json = $$"""
                [{"Path":"{{EscapeJson(absolutePath)}}","LineNumber":1,"Line":"class Program {}"}]
                """;
            return new ProcessExecutionResult(0, json, string.Empty);
        }

        return new ProcessExecutionResult(
            0,
            $"{absolutePath}:1:class Program {{}}",
            string.Empty);
    }

    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal);
    }

    private sealed class StubWorkspaceRootProvider : IWorkspaceRootProvider
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
