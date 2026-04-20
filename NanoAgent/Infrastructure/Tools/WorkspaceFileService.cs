using System.Text;
using System.Text.Json;
using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Tools.Models;
using NanoAgent.Infrastructure.Secrets;

namespace NanoAgent.Infrastructure.Tools;

internal sealed class WorkspaceFileService : IWorkspaceFileService
{
    private const int MaxDirectoryEntries = 200;
    private const int MaxFileReadBytes = 262_144;
    private const int MaxSearchFileBytes = 262_144;
    private const int MaxSearchResults = 100;
    private const int MaxFileSearchResults = 200;
    private const int FileWritePreviewContextLines = 1;
    private const int MaxFileWritePreviewLines = 8;

    private readonly IProcessRunner _processRunner;
    private readonly IWorkspaceRootProvider _workspaceRootProvider;

    public WorkspaceFileService(
        IWorkspaceRootProvider workspaceRootProvider,
        IProcessRunner processRunner)
    {
        _workspaceRootProvider = workspaceRootProvider;
        _processRunner = processRunner;
    }

    public async Task<WorkspaceApplyPatchResult> ApplyPatchAsync(
        string patch,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PatchDocument document = ParsePatch(patch);
        List<WorkspaceApplyPatchFileResult> files = [];

        foreach (PatchOperation operation in document.Operations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            WorkspaceApplyPatchFileResult result = operation.Kind switch
            {
                PatchOperationKind.Add => await ApplyAddFileOperationAsync(operation, cancellationToken),
                PatchOperationKind.Delete => await ApplyDeleteFileOperationAsync(operation, cancellationToken),
                PatchOperationKind.Update => await ApplyUpdateFileOperationAsync(operation, cancellationToken),
                _ => throw new InvalidOperationException("Unsupported patch operation.")
            };

            files.Add(result);
        }

        return new WorkspaceApplyPatchResult(
            files.Count,
            files.Sum(static file => file.AddedLineCount),
            files.Sum(static file => file.RemovedLineCount),
            files);
    }

    public async Task<WorkspaceDirectoryListResult> ListDirectoryAsync(
        string? path,
        bool recursive,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = ResolveWorkspacePath(path, directoryRequired: true, fileRequired: false);
        WorkspaceDirectoryEntry[] entries = await TryListDirectoryWithShellAsync(
            fullPath,
            recursive,
            cancellationToken) ?? ListDirectoryManaged(fullPath, recursive);

        return new WorkspaceDirectoryListResult(
            ToWorkspaceRelativePath(fullPath),
            entries);
    }

    public async Task<WorkspaceFileReadResult> ReadFileAsync(
        string path,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = ResolveWorkspacePath(path, directoryRequired: false, fileRequired: true);
        FileInfo fileInfo = new(fullPath);
        if (fileInfo.Length > MaxFileReadBytes)
        {
            throw new InvalidOperationException(
                $"File '{ToWorkspaceRelativePath(fullPath)}' exceeds the maximum readable size of {MaxFileReadBytes} bytes.");
        }

        string content = await TryReadFileWithShellAsync(fullPath, cancellationToken) ??
                         await File.ReadAllTextAsync(
                             fullPath,
                             Encoding.UTF8,
                             cancellationToken);

        return new WorkspaceFileReadResult(
            ToWorkspaceRelativePath(fullPath),
            content,
            content.Length);
    }

    public async Task<WorkspaceFileSearchResult> SearchFilesAsync(
        WorkspaceFileSearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = ResolveWorkspacePath(request.Path, directoryRequired: false, fileRequired: false);
        IReadOnlyList<string> matches = await TrySearchFilesWithShellAsync(
            request,
            fullPath,
            cancellationToken) ?? SearchFilesManaged(request, fullPath);

        return new WorkspaceFileSearchResult(
            request.Query,
            ToWorkspaceRelativePath(fullPath),
            matches);
    }

    public async Task<WorkspaceTextSearchResult> SearchTextAsync(
        WorkspaceTextSearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = ResolveWorkspacePath(request.Path, directoryRequired: false, fileRequired: false);
        IReadOnlyList<WorkspaceTextSearchMatch> matches = await TrySearchTextWithShellAsync(
            request,
            fullPath,
            cancellationToken) ?? await SearchTextManagedAsync(fullPath, request, cancellationToken);

        return new WorkspaceTextSearchResult(
            request.Query,
            ToWorkspaceRelativePath(fullPath),
            matches);
    }

    public async Task<WorkspaceFileWriteResult> WriteFileAsync(
        string path,
        string content,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "File content must not be empty.");
        }

        string fullPath = ResolveWorkspacePath(path, directoryRequired: false, fileRequired: false);
        bool fileExists = File.Exists(fullPath);
        string? previousContent = null;
        if (fileExists && !overwrite)
        {
            throw new InvalidOperationException(
                $"File '{ToWorkspaceRelativePath(fullPath)}' already exists and overwrite is disabled.");
        }

        if (fileExists)
        {
            previousContent = await File.ReadAllTextAsync(
                fullPath,
                Encoding.UTF8,
                cancellationToken);
        }

        EnsureParentDirectory(fullPath);

        await File.WriteAllTextAsync(
            fullPath,
            content,
            Encoding.UTF8,
            cancellationToken);

        FileWritePreview preview = BuildFileWritePreview(previousContent, content);

        return new WorkspaceFileWriteResult(
            ToWorkspaceRelativePath(fullPath),
            fileExists,
            content.Length,
            preview.AddedLineCount,
            preview.RemovedLineCount,
            preview.Lines,
            preview.RemainingPreviewLineCount);
    }

    private async Task<WorkspaceApplyPatchFileResult> ApplyAddFileOperationAsync(
        PatchOperation operation,
        CancellationToken cancellationToken)
    {
        string fullPath = ResolveWorkspacePath(operation.Path, directoryRequired: false, fileRequired: false);
        if (File.Exists(fullPath))
        {
            throw new InvalidOperationException(
                $"Cannot add '{operation.Path}' because the file already exists.");
        }

        string content = JoinLines(operation.AddLines, trailingNewLine: false);
        EnsureParentDirectory(fullPath);
        await File.WriteAllTextAsync(
            fullPath,
            content,
            Encoding.UTF8,
            cancellationToken);

        return CreatePatchFileResult(
            fullPath,
            previousPath: null,
            "add",
            previousContent: null,
            currentContent: content);
    }

    private async Task<WorkspaceApplyPatchFileResult> ApplyDeleteFileOperationAsync(
        PatchOperation operation,
        CancellationToken cancellationToken)
    {
        string fullPath = ResolveWorkspacePath(operation.Path, directoryRequired: false, fileRequired: true);
        string previousContent = await File.ReadAllTextAsync(
            fullPath,
            Encoding.UTF8,
            cancellationToken);

        File.Delete(fullPath);

        return CreatePatchFileResult(
            fullPath,
            previousPath: null,
            "delete",
            previousContent,
            string.Empty);
    }

    private async Task<WorkspaceApplyPatchFileResult> ApplyUpdateFileOperationAsync(
        PatchOperation operation,
        CancellationToken cancellationToken)
    {
        string currentFullPath = ResolveWorkspacePath(operation.Path, directoryRequired: false, fileRequired: true);
        string previousContent = await File.ReadAllTextAsync(
            currentFullPath,
            Encoding.UTF8,
            cancellationToken);
        string updatedContent = ApplyUpdatePatch(operation.Path, previousContent, operation.Hunks);

        string destinationFullPath = operation.MoveToPath is null
            ? currentFullPath
            : ResolveWorkspacePath(operation.MoveToPath, directoryRequired: false, fileRequired: false);

        if (!string.Equals(currentFullPath, destinationFullPath, GetPathComparison()) &&
            File.Exists(destinationFullPath))
        {
            throw new InvalidOperationException(
                $"Cannot move '{operation.Path}' to '{operation.MoveToPath}' because the destination already exists.");
        }

        EnsureParentDirectory(destinationFullPath);

        await File.WriteAllTextAsync(
            destinationFullPath,
            updatedContent,
            Encoding.UTF8,
            cancellationToken);

        if (!string.Equals(currentFullPath, destinationFullPath, GetPathComparison()) &&
            File.Exists(currentFullPath))
        {
            File.Delete(currentFullPath);
        }

        return CreatePatchFileResult(
            destinationFullPath,
            operation.MoveToPath is null
                ? null
                : ToWorkspaceRelativePath(currentFullPath),
            operation.MoveToPath is null ? "update" : "move",
            previousContent,
            updatedContent);
    }

    private WorkspaceApplyPatchFileResult CreatePatchFileResult(
        string fullPath,
        string? previousPath,
        string operation,
        string? previousContent,
        string currentContent)
    {
        FileWritePreview preview = BuildFileWritePreview(previousContent, currentContent);

        return new WorkspaceApplyPatchFileResult(
            ToWorkspaceRelativePath(fullPath),
            operation,
            previousPath,
            preview.AddedLineCount,
            preview.RemovedLineCount,
            preview.Lines,
            preview.RemainingPreviewLineCount);
    }

    private WorkspaceDirectoryEntry[] ListDirectoryManaged(
        string fullPath,
        bool recursive)
    {
        IEnumerable<string> entries = recursive
            ? Directory.EnumerateFileSystemEntries(fullPath, "*", SearchOption.AllDirectories)
            : Directory.EnumerateFileSystemEntries(fullPath, "*", SearchOption.TopDirectoryOnly);

        return entries
            .OrderBy(static entry => entry, StringComparer.Ordinal)
            .Take(MaxDirectoryEntries)
            .Select(entry => new WorkspaceDirectoryEntry(
                ToWorkspaceRelativePath(entry),
                Directory.Exists(entry) ? "directory" : "file"))
            .ToArray();
    }

    private IReadOnlyList<string> SearchFilesManaged(
        WorkspaceFileSearchRequest request,
        string fullPath)
    {
        StringComparison comparison = request.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        IEnumerable<string> files = File.Exists(fullPath)
            ? [fullPath]
            : Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories);

        return files
            .OrderBy(static path => path, StringComparer.Ordinal)
            .Select(filePath => ToWorkspaceRelativePath(filePath))
            .Where(relativePath => relativePath.Contains(request.Query, comparison))
            .Take(MaxFileSearchResults)
            .ToArray();
    }

    private async Task<IReadOnlyList<WorkspaceTextSearchMatch>> SearchTextManagedAsync(
        string fullPath,
        WorkspaceTextSearchRequest request,
        CancellationToken cancellationToken)
    {
        List<string> filesToSearch = [];

        if (File.Exists(fullPath))
        {
            filesToSearch.Add(fullPath);
        }
        else if (Directory.Exists(fullPath))
        {
            filesToSearch.AddRange(Directory.EnumerateFiles(
                fullPath,
                "*",
                SearchOption.AllDirectories));
        }
        else
        {
            throw new FileNotFoundException(
                $"Search path '{request.Path ?? "."}' does not exist.");
        }

        List<WorkspaceTextSearchMatch> matches = [];
        StringComparison comparison = request.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        foreach (string filePath in filesToSearch.OrderBy(static path => path, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();

            FileInfo fileInfo = new(filePath);
            if (fileInfo.Length > MaxSearchFileBytes)
            {
                continue;
            }

            string[] lines;
            try
            {
                lines = await File.ReadAllLinesAsync(
                    filePath,
                    Encoding.UTF8,
                    cancellationToken);
            }
            catch (DecoderFallbackException)
            {
                continue;
            }
            catch (InvalidDataException)
            {
                continue;
            }

            for (int index = 0; index < lines.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!lines[index].Contains(request.Query, comparison))
                {
                    continue;
                }

                matches.Add(new WorkspaceTextSearchMatch(
                    ToWorkspaceRelativePath(filePath),
                    index + 1,
                    lines[index].Trim()));

                if (matches.Count >= MaxSearchResults)
                {
                    return matches;
                }
            }
        }

        return matches;
    }

    private async Task<WorkspaceDirectoryEntry[]?> TryListDirectoryWithShellAsync(
        string fullPath,
        bool recursive,
        CancellationToken cancellationToken)
    {
        ProcessExecutionResult? result = await TryRunWorkspaceShellAsync(
            BuildListDirectoryCommand(fullPath, recursive),
            cancellationToken);

        if (result is null || result.ExitCode != 0)
        {
            return null;
        }

        IEnumerable<string> paths = NormalizeOutputLines(result.StandardOutput);
        if (!recursive)
        {
            paths = paths.Where(path => IsDirectChild(fullPath, path));
        }

        return paths
            .Distinct(GetPathComparer())
            .Take(MaxDirectoryEntries)
            .Select(path => new WorkspaceDirectoryEntry(
                ToWorkspaceRelativePath(path),
                Directory.Exists(path) ? "directory" : "file"))
            .ToArray();
    }

    private async Task<string?> TryReadFileWithShellAsync(
        string fullPath,
        CancellationToken cancellationToken)
    {
        ProcessExecutionResult? result = await TryRunWorkspaceShellAsync(
            BuildReadFileCommand(fullPath),
            cancellationToken);

        return result is { ExitCode: 0 }
            ? result.StandardOutput
            : null;
    }

    private async Task<IReadOnlyList<string>?> TrySearchFilesWithShellAsync(
        WorkspaceFileSearchRequest request,
        string fullPath,
        CancellationToken cancellationToken)
    {
        if (File.Exists(fullPath))
        {
            return SearchFilesManaged(request, fullPath);
        }

        ProcessExecutionResult? result = await TryRunWorkspaceShellAsync(
            BuildSearchFilesCommand(fullPath, request),
            cancellationToken);

        if (result is null)
        {
            return null;
        }

        if (!OperatingSystem.IsWindows() && result.ExitCode == 1)
        {
            return [];
        }

        if (result.ExitCode != 0)
        {
            return null;
        }

        return NormalizeOutputLines(result.StandardOutput)
            .Distinct(GetPathComparer())
            .Select(path => ToWorkspaceRelativePath(path))
            .Take(MaxFileSearchResults)
            .ToArray();
    }

    private async Task<IReadOnlyList<WorkspaceTextSearchMatch>?> TrySearchTextWithShellAsync(
        WorkspaceTextSearchRequest request,
        string fullPath,
        CancellationToken cancellationToken)
    {
        ProcessExecutionResult? result = await TryRunWorkspaceShellAsync(
            BuildTextSearchCommand(fullPath, request),
            cancellationToken);

        if (result is null)
        {
            return null;
        }

        if (!OperatingSystem.IsWindows() && result.ExitCode == 1)
        {
            return [];
        }

        if (result.ExitCode != 0)
        {
            return null;
        }

        return OperatingSystem.IsWindows()
            ? ParsePowerShellSearchMatches(result.StandardOutput)
            : ParseGrepSearchMatches(result.StandardOutput);
    }

    private async Task<ProcessExecutionResult?> TryRunWorkspaceShellAsync(
        string command,
        CancellationToken cancellationToken)
    {
        try
        {
            return await RunWorkspaceShellAsync(command, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private Task<ProcessExecutionResult> RunWorkspaceShellAsync(
        string command,
        CancellationToken cancellationToken)
    {
        string workspaceRoot = Path.GetFullPath(_workspaceRootProvider.GetWorkspaceRoot());
        ProcessExecutionRequest request = OperatingSystem.IsWindows()
            ? new ProcessExecutionRequest(
                "powershell",
                ["-NoProfile", "-NonInteractive", "-Command", command],
                WorkingDirectory: workspaceRoot)
            : new ProcessExecutionRequest(
                "/bin/bash",
                ["-lc", command],
                WorkingDirectory: workspaceRoot);

        return _processRunner.RunAsync(request, cancellationToken);
    }

    private static string BuildListDirectoryCommand(
        string fullPath,
        bool recursive)
    {
        return OperatingSystem.IsWindows()
            ? $"Get-ChildItem -LiteralPath '{EscapePowerShellSingleQuotedString(fullPath)}' -Force{(recursive ? " -Recurse" : string.Empty)} | Sort-Object FullName | Select-Object -ExpandProperty FullName"
            : $"find '{EscapeBashSingleQuotedString(fullPath)}' -mindepth 1 | sort";
    }

    private static string BuildReadFileCommand(string fullPath)
    {
        return OperatingSystem.IsWindows()
            ? $"Get-Content -LiteralPath '{EscapePowerShellSingleQuotedString(fullPath)}' -Raw"
            : $"cat -- '{EscapeBashSingleQuotedString(fullPath)}'";
    }

    private static string BuildSearchFilesCommand(
        string fullPath,
        WorkspaceFileSearchRequest request)
    {
        return OperatingSystem.IsWindows()
            ? BuildPowerShellFileSearchCommand(fullPath, request)
            : BuildBashFileSearchCommand(fullPath, request);
    }

    private static string BuildTextSearchCommand(
        string fullPath,
        WorkspaceTextSearchRequest request)
    {
        return OperatingSystem.IsWindows()
            ? BuildPowerShellTextSearchCommand(fullPath, request)
            : BuildBashTextSearchCommand(fullPath, request);
    }

    private static string BuildPowerShellFileSearchCommand(
        string fullPath,
        WorkspaceFileSearchRequest request)
    {
        string comparison = request.CaseSensitive ? "Ordinal" : "OrdinalIgnoreCase";
        string escapedPath = EscapePowerShellSingleQuotedString(fullPath);
        string escapedQuery = EscapePowerShellSingleQuotedString(request.Query);

        return $$"""
            $root = [System.IO.Path]::GetFullPath('{{escapedPath}}')
            $comparison = [System.StringComparison]::{{comparison}}
            Get-ChildItem -LiteralPath $root -File -Recurse -Force |
            Where-Object {
              $relative = [System.IO.Path]::GetRelativePath($root, $_.FullName)
              $relative.IndexOf('{{escapedQuery}}', $comparison) -ge 0
            } |
            Sort-Object FullName |
            Select-Object -First {{MaxFileSearchResults}} |
            Select-Object -ExpandProperty FullName
            """;
    }

    private static string BuildBashFileSearchCommand(
        string fullPath,
        WorkspaceFileSearchRequest request)
    {
        string ignoreCaseFlag = request.CaseSensitive ? string.Empty : "-i ";
        return $"find '{EscapeBashSingleQuotedString(fullPath)}' -type f | sort | grep {ignoreCaseFlag}-F -- '{EscapeBashSingleQuotedString(request.Query)}' | head -n {MaxFileSearchResults}";
    }

    private static string BuildPowerShellTextSearchCommand(
        string fullPath,
        WorkspaceTextSearchRequest request)
    {
        string escapedPath = EscapePowerShellSingleQuotedString(fullPath);
        string escapedQuery = EscapePowerShellSingleQuotedString(request.Query);
        string caseSensitive = request.CaseSensitive ? "$true" : "$false";

        if (File.Exists(fullPath))
        {
            return $$"""
                $matches = if ((Get-Item -LiteralPath '{{escapedPath}}').Length -le {{MaxSearchFileBytes}}) {
                  @(Select-String -LiteralPath '{{escapedPath}}' -SimpleMatch -Pattern '{{escapedQuery}}' -CaseSensitive:{{caseSensitive}})
                }
                else {
                  @()
                }
                @($matches | Select-Object -First {{MaxSearchResults}} Path, LineNumber, Line) | ConvertTo-Json -Compress
                """;
        }

        return $$"""
            $matches = @(
              Get-ChildItem -LiteralPath '{{escapedPath}}' -File -Recurse -Force |
              Where-Object { $_.Length -le {{MaxSearchFileBytes}} } |
              Select-String -SimpleMatch -Pattern '{{escapedQuery}}' -CaseSensitive:{{caseSensitive}}
            )
            @($matches | Select-Object -First {{MaxSearchResults}} Path, LineNumber, Line) | ConvertTo-Json -Compress
            """;
    }

    private static string BuildBashTextSearchCommand(
        string fullPath,
        WorkspaceTextSearchRequest request)
    {
        string ignoreCaseFlag = request.CaseSensitive ? string.Empty : "-i ";
        string flags = $"-n -I -F {ignoreCaseFlag}".Trim();
        string escapedQuery = EscapeBashSingleQuotedString(request.Query);
        string escapedPath = EscapeBashSingleQuotedString(fullPath);

        return File.Exists(fullPath)
            ? $"grep {flags} -- '{escapedQuery}' '{escapedPath}' | head -n {MaxSearchResults}"
            : $"grep -R {flags} -- '{escapedQuery}' '{escapedPath}' | head -n {MaxSearchResults}";
    }

    private IReadOnlyList<WorkspaceTextSearchMatch> ParsePowerShellSearchMatches(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        using JsonDocument document = JsonDocument.Parse(output);
        JsonElement root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root.EnumerateArray()
                .Select(CreatePowerShellSearchMatch)
                .Where(static match => match is not null)
                .Cast<WorkspaceTextSearchMatch>()
                .ToArray();
        }

        WorkspaceTextSearchMatch? singleMatch = CreatePowerShellSearchMatch(root);
        return singleMatch is null ? [] : [singleMatch];
    }

    private WorkspaceTextSearchMatch? CreatePowerShellSearchMatch(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty("Path", out JsonElement pathElement) ||
            !element.TryGetProperty("LineNumber", out JsonElement lineNumberElement) ||
            !element.TryGetProperty("Line", out JsonElement lineElement) ||
            pathElement.ValueKind != JsonValueKind.String ||
            !lineNumberElement.TryGetInt32(out int lineNumber) ||
            lineElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return new WorkspaceTextSearchMatch(
            ToWorkspaceRelativePath(pathElement.GetString() ?? string.Empty),
            lineNumber,
            (lineElement.GetString() ?? string.Empty).Trim());
    }

    private IReadOnlyList<WorkspaceTextSearchMatch> ParseGrepSearchMatches(string output)
    {
        List<WorkspaceTextSearchMatch> matches = [];

        foreach (string line in NormalizeOutputLines(output))
        {
            if (!TryParseGrepOutputLine(line, out string? path, out int lineNumber, out string? lineText))
            {
                continue;
            }

            string safePath = path!;
            string normalizedPath = Path.IsPathRooted(safePath)
                ? ToWorkspaceRelativePath(safePath)
                : safePath.Replace('\\', '/');

            matches.Add(new WorkspaceTextSearchMatch(
                normalizedPath,
                lineNumber,
                lineText!.Trim()));
        }

        return matches;
    }

    private static bool TryParseGrepOutputLine(
        string value,
        out string? path,
        out int lineNumber,
        out string? lineText)
    {
        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] != ':')
            {
                continue;
            }

            int nextColonIndex = value.IndexOf(':', index + 1);
            if (nextColonIndex < 0)
            {
                break;
            }

            ReadOnlySpan<char> lineNumberSpan = value.AsSpan(index + 1, nextColonIndex - index - 1);
            if (!int.TryParse(lineNumberSpan, out lineNumber))
            {
                continue;
            }

            path = value[..index];
            lineText = value[(nextColonIndex + 1)..];
            return true;
        }

        path = null;
        lineNumber = 0;
        lineText = null;
        return false;
    }

    private static IEnumerable<string> NormalizeOutputLines(string output)
    {
        return output
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(static line => line.Trim())
            .Where(static line => !string.IsNullOrWhiteSpace(line));
    }

    private static bool IsDirectChild(
        string parentPath,
        string candidatePath)
    {
        string? candidateParent = Path.GetDirectoryName(candidatePath);
        return candidateParent is not null &&
               string.Equals(
                   Path.GetFullPath(candidateParent),
                   Path.GetFullPath(parentPath),
                   GetPathComparison());
    }

    private static StringComparer GetPathComparer()
    {
        return OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    }

    private string ResolveWorkspacePath(
        string? requestedPath,
        bool directoryRequired,
        bool fileRequired)
    {
        string workspaceRoot = Path.GetFullPath(_workspaceRootProvider.GetWorkspaceRoot());
        string normalizedRequestedPath = string.IsNullOrWhiteSpace(requestedPath)
            ? workspaceRoot
            : requestedPath.Trim();

        string fullPath = Path.GetFullPath(
            Path.IsPathRooted(normalizedRequestedPath)
                ? normalizedRequestedPath
                : Path.Combine(workspaceRoot, normalizedRequestedPath));

        EnsureWithinWorkspace(workspaceRoot, fullPath);

        if (directoryRequired && !Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException(
                $"Directory '{ToWorkspaceRelativePath(fullPath)}' does not exist.");
        }

        if (fileRequired && !File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"File '{ToWorkspaceRelativePath(fullPath)}' does not exist.");
        }

        return fullPath;
    }

    private string ToWorkspaceRelativePath(string fullPath)
    {
        string workspaceRoot = Path.GetFullPath(_workspaceRootProvider.GetWorkspaceRoot());
        if (string.Equals(workspaceRoot, fullPath, GetPathComparison()))
        {
            return ".";
        }

        return Path.GetRelativePath(workspaceRoot, fullPath)
            .Replace('\\', '/');
    }

    private static void EnsureWithinWorkspace(
        string workspaceRoot,
        string candidatePath)
    {
        string normalizedRoot = EnsureTrailingSeparator(workspaceRoot);
        string normalizedCandidate = EnsureTrailingSeparator(candidatePath);

        if (!normalizedCandidate.StartsWith(
                normalizedRoot,
                GetPathComparison()) &&
            !string.Equals(workspaceRoot, candidatePath, GetPathComparison()))
        {
            throw new InvalidOperationException(
                "Tool paths must stay within the current workspace.");
        }
    }

    private static StringComparison GetPathComparison()
    {
        return OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) ||
               path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    private static string EscapePowerShellSingleQuotedString(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private static string EscapeBashSingleQuotedString(string value)
    {
        return value.Replace("'", "'\"'\"'", StringComparison.Ordinal);
    }

    private static void EnsureParentDirectory(string fullPath)
    {
        string? directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    private static PatchDocument ParsePatch(string patch)
    {
        if (string.IsNullOrWhiteSpace(patch))
        {
            throw new FormatException("Patch text must not be empty.");
        }

        string[] lines = patch
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.None);

        int lineIndex = 0;
        SkipEmptyLines(lines, ref lineIndex);

        if (lineIndex >= lines.Length || !string.Equals(lines[lineIndex], "*** Begin Patch", StringComparison.Ordinal))
        {
            throw new FormatException("Patch text must begin with '*** Begin Patch'.");
        }

        lineIndex++;
        List<PatchOperation> operations = [];

        while (lineIndex < lines.Length)
        {
            string currentLine = lines[lineIndex];
            if (string.Equals(currentLine, "*** End Patch", StringComparison.Ordinal))
            {
                return new PatchDocument(operations);
            }

            if (string.IsNullOrWhiteSpace(currentLine))
            {
                lineIndex++;
                continue;
            }

            if (currentLine.StartsWith("*** Add File: ", StringComparison.Ordinal))
            {
                operations.Add(ParseAddFile(lines, ref lineIndex));
                continue;
            }

            if (currentLine.StartsWith("*** Delete File: ", StringComparison.Ordinal))
            {
                operations.Add(ParseDeleteFile(lines, ref lineIndex));
                continue;
            }

            if (currentLine.StartsWith("*** Update File: ", StringComparison.Ordinal))
            {
                operations.Add(ParseUpdateFile(lines, ref lineIndex));
                continue;
            }

            throw new FormatException($"Unrecognized patch line: '{currentLine}'.");
        }

        throw new FormatException("Patch text must end with '*** End Patch'.");
    }

    private static PatchOperation ParseAddFile(
        IReadOnlyList<string> lines,
        ref int lineIndex)
    {
        string path = ParseHeaderValue(lines[lineIndex], "*** Add File: ");
        lineIndex++;

        List<string> fileLines = [];
        while (lineIndex < lines.Count &&
               !lines[lineIndex].StartsWith("*** ", StringComparison.Ordinal))
        {
            if (lines[lineIndex].Length == 0)
            {
                throw new FormatException("Add file patch lines must start with '+'.");
            }

            if (lines[lineIndex][0] != '+')
            {
                throw new FormatException("Add file patch lines must start with '+'.");
            }

            fileLines.Add(lines[lineIndex][1..]);
            lineIndex++;
        }

        if (fileLines.Count == 0)
        {
            throw new FormatException("Add file operations must include at least one '+' line.");
        }

        return new PatchOperation(
            PatchOperationKind.Add,
            path,
            MoveToPath: null,
            fileLines,
            []);
    }

    private static PatchOperation ParseDeleteFile(
        IReadOnlyList<string> lines,
        ref int lineIndex)
    {
        string path = ParseHeaderValue(lines[lineIndex], "*** Delete File: ");
        lineIndex++;

        return new PatchOperation(
            PatchOperationKind.Delete,
            path,
            MoveToPath: null,
            [],
            []);
    }

    private static PatchOperation ParseUpdateFile(
        IReadOnlyList<string> lines,
        ref int lineIndex)
    {
        string path = ParseHeaderValue(lines[lineIndex], "*** Update File: ");
        lineIndex++;

        string? moveToPath = null;
        if (lineIndex < lines.Count &&
            lines[lineIndex].StartsWith("*** Move to: ", StringComparison.Ordinal))
        {
            moveToPath = ParseHeaderValue(lines[lineIndex], "*** Move to: ");
            lineIndex++;
        }

        List<PatchHunk> hunks = [];
        List<PatchLine>? currentHunkLines = null;

        while (lineIndex < lines.Count &&
               !lines[lineIndex].StartsWith("*** ", StringComparison.Ordinal))
        {
            string line = lines[lineIndex];
            if (string.Equals(line, "\\ No newline at end of file", StringComparison.Ordinal) ||
                string.Equals(line, "*** End of File", StringComparison.Ordinal))
            {
                lineIndex++;
                continue;
            }

            if (line.StartsWith("@@", StringComparison.Ordinal))
            {
                if (currentHunkLines is not null)
                {
                    hunks.Add(new PatchHunk(currentHunkLines));
                }

                currentHunkLines = [];
                lineIndex++;
                continue;
            }

            if (line.Length == 0 || line[0] is not (' ' or '+' or '-'))
            {
                throw new FormatException($"Invalid update patch line: '{line}'.");
            }

            currentHunkLines ??= [];
            currentHunkLines.Add(new PatchLine(
                line[0] switch
                {
                    ' ' => PatchLineKind.Context,
                    '+' => PatchLineKind.Addition,
                    '-' => PatchLineKind.Removal,
                    _ => throw new FormatException($"Invalid patch line prefix in '{line}'.")
                },
                line[1..]));

            lineIndex++;
        }

        if (currentHunkLines is not null)
        {
            hunks.Add(new PatchHunk(currentHunkLines));
        }

        if (moveToPath is null && hunks.Count == 0)
        {
            throw new FormatException("Update file operations must include at least one hunk or a move target.");
        }

        return new PatchOperation(
            PatchOperationKind.Update,
            path,
            moveToPath,
            [],
            hunks);
    }

    private static void SkipEmptyLines(
        IReadOnlyList<string> lines,
        ref int lineIndex)
    {
        while (lineIndex < lines.Count &&
               string.IsNullOrWhiteSpace(lines[lineIndex]))
        {
            lineIndex++;
        }
    }

    private static string ParseHeaderValue(
        string line,
        string prefix)
    {
        string value = line[prefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException($"Patch header '{prefix.Trim()}' must include a path.");
        }

        return value;
    }

    private static string ApplyUpdatePatch(
        string path,
        string previousContent,
        IReadOnlyList<PatchHunk> hunks)
    {
        List<string> currentLines = SplitLines(previousContent).ToList();
        int searchStart = 0;

        foreach (PatchHunk hunk in hunks)
        {
            string[] beforeLines = hunk.Lines
                .Where(static line => line.Kind is PatchLineKind.Context or PatchLineKind.Removal)
                .Select(static line => line.Text)
                .ToArray();
            string[] afterLines = hunk.Lines
                .Where(static line => line.Kind is PatchLineKind.Context or PatchLineKind.Addition)
                .Select(static line => line.Text)
                .ToArray();

            int matchIndex = beforeLines.Length == 0
                ? searchStart
                : FindSequence(currentLines, beforeLines, searchStart);

            if (matchIndex < 0 && beforeLines.Length > 0 && searchStart > 0)
            {
                matchIndex = FindSequence(currentLines, beforeLines, 0);
            }

            if (matchIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Could not apply the requested patch because the target context was not found in '{path}'.");
            }

            currentLines.RemoveRange(matchIndex, beforeLines.Length);
            currentLines.InsertRange(matchIndex, afterLines);
            searchStart = matchIndex + afterLines.Length;
        }

        bool trailingNewLine = previousContent.EndsWith('\n') || previousContent.EndsWith('\r');
        return JoinLines(currentLines, trailingNewLine);
    }

    private static int FindSequence(
        IReadOnlyList<string> source,
        IReadOnlyList<string> target,
        int startIndex)
    {
        if (target.Count == 0)
        {
            return startIndex;
        }

        for (int index = Math.Max(0, startIndex); index <= source.Count - target.Count; index++)
        {
            bool matched = true;
            for (int offset = 0; offset < target.Count; offset++)
            {
                if (!string.Equals(source[index + offset], target[offset], StringComparison.Ordinal))
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return index;
            }
        }

        return -1;
    }

    private static string JoinLines(
        IEnumerable<string> lines,
        bool trailingNewLine)
    {
        string content = string.Join("\n", lines);
        return trailingNewLine && content.Length > 0
            ? content + "\n"
            : content;
    }

    private static FileWritePreview BuildFileWritePreview(
        string? previousContent,
        string currentContent)
    {
        string[] previousLines = SplitLines(previousContent);
        string[] currentLines = SplitLines(currentContent);
        IReadOnlyList<DiffLine> diffLines = previousContent is null
            ? currentLines
                .Select((line, index) => new DiffLine(
                    DiffLineKind.Addition,
                    null,
                    index + 1,
                    line))
                .ToArray()
            : ComputeDiff(previousLines, currentLines);

        int addedLineCount = diffLines.Count(static line => line.Kind == DiffLineKind.Addition);
        int removedLineCount = diffLines.Count(static line => line.Kind == DiffLineKind.Removal);

        if (diffLines.Count == 0)
        {
            return new FileWritePreview(0, 0, [], 0);
        }

        DiffLine[] previewDiffLines = SelectPreviewLines(diffLines)
            .Take(MaxFileWritePreviewLines)
            .ToArray();

        WorkspaceFileWritePreviewLine[] previewLines = previewDiffLines
            .Select(static line => new WorkspaceFileWritePreviewLine(
                line.LineNumber ?? 0,
                line.Kind switch
                {
                    DiffLineKind.Addition => "add",
                    DiffLineKind.Removal => "remove",
                    _ => "context"
                },
                line.Text))
            .ToArray();

        int remainingPreviewLineCount = Math.Max(
            0,
            SelectPreviewLines(diffLines).Count - previewLines.Length);

        return new FileWritePreview(
            addedLineCount,
            removedLineCount,
            previewLines,
            remainingPreviewLineCount);
    }

    private static string[] SplitLines(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return [];
        }

        string[] rawLines = content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.None);

        if (rawLines.Length > 0 && rawLines[^1].Length == 0)
        {
            return rawLines[..^1];
        }

        return rawLines;
    }

    private static IReadOnlyList<DiffLine> ComputeDiff(
        IReadOnlyList<string> previousLines,
        IReadOnlyList<string> currentLines)
    {
        if (previousLines.Count == 0 && currentLines.Count == 0)
        {
            return [];
        }

        int max = previousLines.Count + currentLines.Count;
        Dictionary<int, int> frontier = new() { [1] = 0 };
        List<Dictionary<int, int>> trace = [];

        for (int distance = 0; distance <= max; distance++)
        {
            trace.Add(new Dictionary<int, int>(frontier));

            for (int diagonal = -distance; diagonal <= distance; diagonal += 2)
            {
                int x;
                if (diagonal == -distance ||
                    (diagonal != distance &&
                     GetFrontierValue(frontier, diagonal - 1) < GetFrontierValue(frontier, diagonal + 1)))
                {
                    x = GetFrontierValue(frontier, diagonal + 1);
                }
                else
                {
                    x = GetFrontierValue(frontier, diagonal - 1) + 1;
                }

                int y = x - diagonal;

                while (x < previousLines.Count &&
                       y < currentLines.Count &&
                       string.Equals(previousLines[x], currentLines[y], StringComparison.Ordinal))
                {
                    x++;
                    y++;
                }

                frontier[diagonal] = x;

                if (x >= previousLines.Count && y >= currentLines.Count)
                {
                    return Backtrack(trace, previousLines, currentLines);
                }
            }
        }

        return [];
    }

    private static IReadOnlyList<DiffLine> Backtrack(
        IReadOnlyList<Dictionary<int, int>> trace,
        IReadOnlyList<string> previousLines,
        IReadOnlyList<string> currentLines)
    {
        List<DiffLine> lines = [];
        int x = previousLines.Count;
        int y = currentLines.Count;

        for (int distance = trace.Count - 1; distance >= 0; distance--)
        {
            Dictionary<int, int> frontier = trace[distance];
            int diagonal = x - y;

            int previousDiagonal;
            if (diagonal == -distance ||
                (diagonal != distance &&
                 GetFrontierValue(frontier, diagonal - 1) < GetFrontierValue(frontier, diagonal + 1)))
            {
                previousDiagonal = diagonal + 1;
            }
            else
            {
                previousDiagonal = diagonal - 1;
            }

            int previousX = GetFrontierValue(frontier, previousDiagonal);
            int previousY = previousX - previousDiagonal;

            while (x > previousX && y > previousY)
            {
                lines.Add(new DiffLine(
                    DiffLineKind.Context,
                    x,
                    y,
                    previousLines[x - 1]));
                x--;
                y--;
            }

            if (distance == 0)
            {
                break;
            }

            if (x == previousX)
            {
                lines.Add(new DiffLine(
                    DiffLineKind.Addition,
                    null,
                    y,
                    currentLines[y - 1]));
                y--;
            }
            else
            {
                lines.Add(new DiffLine(
                    DiffLineKind.Removal,
                    x,
                    null,
                    previousLines[x - 1]));
                x--;
            }
        }

        lines.Reverse();
        return lines;
    }

    private static int GetFrontierValue(
        IReadOnlyDictionary<int, int> frontier,
        int diagonal)
    {
        return frontier.TryGetValue(diagonal, out int value)
            ? value
            : 0;
    }

    private static IReadOnlyList<DiffLine> SelectPreviewLines(
        IReadOnlyList<DiffLine> diffLines)
    {
        int firstChangedIndex = -1;
        for (int index = 0; index < diffLines.Count; index++)
        {
            if (diffLines[index].Kind != DiffLineKind.Context)
            {
                firstChangedIndex = index;
                break;
            }
        }

        if (firstChangedIndex < 0)
        {
            return [];
        }

        int start = Math.Max(0, firstChangedIndex - FileWritePreviewContextLines);
        int end = firstChangedIndex + 1;
        int trailingContextCount = 0;

        while (end < diffLines.Count)
        {
            if (diffLines[end].Kind == DiffLineKind.Context)
            {
                trailingContextCount++;
                if (trailingContextCount > FileWritePreviewContextLines)
                {
                    break;
                }
            }
            else
            {
                trailingContextCount = 0;
            }

            end++;
        }

        return diffLines
            .Skip(start)
            .Take(end - start)
            .ToArray();
    }

    private readonly record struct PatchDocument(
        IReadOnlyList<PatchOperation> Operations);

    private readonly record struct PatchOperation(
        PatchOperationKind Kind,
        string Path,
        string? MoveToPath,
        IReadOnlyList<string> AddLines,
        IReadOnlyList<PatchHunk> Hunks);

    private readonly record struct PatchHunk(
        IReadOnlyList<PatchLine> Lines);

    private readonly record struct PatchLine(
        PatchLineKind Kind,
        string Text);

    private enum PatchOperationKind
    {
        Add = 0,
        Delete = 1,
        Update = 2
    }

    private enum PatchLineKind
    {
        Context = 0,
        Addition = 1,
        Removal = 2
    }

    private readonly record struct FileWritePreview(
        int AddedLineCount,
        int RemovedLineCount,
        WorkspaceFileWritePreviewLine[] Lines,
        int RemainingPreviewLineCount);

    private readonly record struct DiffLine(
        DiffLineKind Kind,
        int? OriginalLineNumber,
        int? UpdatedLineNumber,
        string Text)
    {
        public int? LineNumber => Kind == DiffLineKind.Removal
            ? OriginalLineNumber
            : UpdatedLineNumber;
    }

    private enum DiffLineKind
    {
        Context = 0,
        Addition = 1,
        Removal = 2
    }
}
