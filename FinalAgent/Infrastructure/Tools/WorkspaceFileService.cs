using System.Text;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Tools.Models;

namespace FinalAgent.Infrastructure.Tools;

internal sealed class WorkspaceFileService : IWorkspaceFileService
{
    private const int MaxDirectoryEntries = 200;
    private const int MaxFileReadBytes = 262_144;
    private const int MaxSearchFileBytes = 262_144;
    private const int MaxSearchResults = 100;

    private readonly IWorkspaceRootProvider _workspaceRootProvider;

    public WorkspaceFileService(IWorkspaceRootProvider workspaceRootProvider)
    {
        _workspaceRootProvider = workspaceRootProvider;
    }

    public Task<WorkspaceDirectoryListResult> ListDirectoryAsync(
        string? path,
        bool recursive,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = ResolveWorkspacePath(path, directoryRequired: true, fileRequired: false);

        IEnumerable<string> entries = recursive
            ? Directory.EnumerateFileSystemEntries(fullPath, "*", SearchOption.AllDirectories)
            : Directory.EnumerateFileSystemEntries(fullPath, "*", SearchOption.TopDirectoryOnly);

        WorkspaceDirectoryEntry[] results = entries
            .OrderBy(static entry => entry, StringComparer.Ordinal)
            .Take(MaxDirectoryEntries)
            .Select(entry => new WorkspaceDirectoryEntry(
                ToWorkspaceRelativePath(entry),
                Directory.Exists(entry) ? "directory" : "file"))
            .ToArray();

        return Task.FromResult(new WorkspaceDirectoryListResult(
            ToWorkspaceRelativePath(fullPath),
            results));
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

        string content = await File.ReadAllTextAsync(
            fullPath,
            Encoding.UTF8,
            cancellationToken);

        return new WorkspaceFileReadResult(
            ToWorkspaceRelativePath(fullPath),
            content,
            content.Length);
    }

    public async Task<WorkspaceTextSearchResult> SearchTextAsync(
        WorkspaceTextSearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = ResolveWorkspacePath(request.Path, directoryRequired: false, fileRequired: false);
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
                    return new WorkspaceTextSearchResult(
                        request.Query,
                        ToWorkspaceRelativePath(fullPath),
                        matches);
                }
            }
        }

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
        if (fileExists && !overwrite)
        {
            throw new InvalidOperationException(
                $"File '{ToWorkspaceRelativePath(fullPath)}' already exists and overwrite is disabled.");
        }

        string? directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await File.WriteAllTextAsync(
            fullPath,
            content,
            Encoding.UTF8,
            cancellationToken);

        return new WorkspaceFileWriteResult(
            ToWorkspaceRelativePath(fullPath),
            fileExists,
            content.Length);
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
}
