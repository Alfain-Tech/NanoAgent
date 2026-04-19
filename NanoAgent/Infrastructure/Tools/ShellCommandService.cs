using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Tools.Models;
using NanoAgent.Infrastructure.Secrets;

namespace NanoAgent.Infrastructure.Tools;

internal sealed class ShellCommandService : IShellCommandService
{
    private const int MaxOutputCharacters = 8_000;

    private readonly IProcessRunner _processRunner;
    private readonly IWorkspaceRootProvider _workspaceRootProvider;

    public ShellCommandService(
        IProcessRunner processRunner,
        IWorkspaceRootProvider workspaceRootProvider)
    {
        _processRunner = processRunner;
        _workspaceRootProvider = workspaceRootProvider;
    }

    public async Task<ShellCommandExecutionResult> ExecuteAsync(
        ShellCommandExecutionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Command))
        {
            throw new ArgumentException(
                "Shell command must be provided.",
                nameof(request));
        }

        string workingDirectory = ResolveWorkspacePath(request.WorkingDirectory, directoryRequired: true);
        ProcessExecutionRequest processRequest = OperatingSystem.IsWindows()
            ? new ProcessExecutionRequest(
                "powershell",
                ["-NoProfile", "-NonInteractive", "-Command", request.Command],
                WorkingDirectory: workingDirectory)
            : new ProcessExecutionRequest(
                "/bin/bash",
                ["-lc", request.Command],
                WorkingDirectory: workingDirectory);

        ProcessExecutionResult result = await _processRunner.RunAsync(
            processRequest,
            cancellationToken);

        return new ShellCommandExecutionResult(
            request.Command,
            ToWorkspaceRelativePath(workingDirectory),
            result.ExitCode,
            TrimOutput(result.StandardOutput),
            TrimOutput(result.StandardError));
    }

    private string ResolveWorkspacePath(
        string? requestedPath,
        bool directoryRequired)
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

    private static string TrimOutput(string value)
    {
        string normalizedValue = value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        if (normalizedValue.Length <= MaxOutputCharacters)
        {
            return normalizedValue;
        }

        return normalizedValue[..MaxOutputCharacters] + "...";
    }
}
