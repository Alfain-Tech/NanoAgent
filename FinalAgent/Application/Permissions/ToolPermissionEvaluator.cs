using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;

namespace FinalAgent.Application.Permissions;

internal sealed class ToolPermissionEvaluator : IPermissionEvaluator
{
    private readonly IWorkspaceRootProvider _workspaceRootProvider;

    public ToolPermissionEvaluator(IWorkspaceRootProvider workspaceRootProvider)
    {
        _workspaceRootProvider = workspaceRootProvider;
    }

    public PermissionEvaluationResult Evaluate(
        ToolPermissionPolicy permissionPolicy,
        PermissionEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(permissionPolicy);
        ArgumentNullException.ThrowIfNull(context);

        if (permissionPolicy.ApprovalMode == ToolApprovalMode.RequireApproval &&
            !context.ApprovalGranted)
        {
            return PermissionEvaluationResult.RequiresApproval(
                "approval_required",
                $"Tool '{context.ToolExecutionContext.ToolName}' requires explicit approval before it can run.");
        }

        foreach (FilePathPermissionRule rule in permissionPolicy.FilePaths)
        {
            PermissionEvaluationResult result = EvaluateFilePathRule(
                context.ToolExecutionContext,
                rule);

            if (!result.IsAllowed)
            {
                return result;
            }
        }

        if (permissionPolicy.Shell is not null)
        {
            PermissionEvaluationResult shellResult = EvaluateShellPolicy(
                context.ToolExecutionContext,
                permissionPolicy.Shell);

            if (!shellResult.IsAllowed)
            {
                return shellResult;
            }
        }

        return PermissionEvaluationResult.Allowed();
    }

    private PermissionEvaluationResult EvaluateFilePathRule(
        ToolExecutionContext context,
        FilePathPermissionRule rule)
    {
        if (!TryGetOptionalString(context.Arguments, rule.ArgumentName, out string? requestedPath))
        {
            return PermissionEvaluationResult.Allowed();
        }

        string workspaceRoot = Path.GetFullPath(_workspaceRootProvider.GetWorkspaceRoot());
        string candidatePath;
        try
        {
            candidatePath = ResolveWithinWorkspace(workspaceRoot, requestedPath!);
        }
        catch (InvalidOperationException)
        {
            return PermissionEvaluationResult.Denied(
                "path_outside_workspace",
                $"Tool '{context.ToolName}' cannot use path '{requestedPath}' because it resolves outside the current workspace.");
        }

        bool isAllowed = rule.AllowedRoots.Any(allowedRoot =>
        {
            string allowedPath = ResolveWithinWorkspace(workspaceRoot, allowedRoot);
            return IsSamePathOrDescendant(allowedPath, candidatePath);
        });

        if (isAllowed)
        {
            return PermissionEvaluationResult.Allowed();
        }

        string allowedRoots = string.Join(", ", rule.AllowedRoots);
        return PermissionEvaluationResult.Denied(
            "path_not_allowed",
            $"Tool '{context.ToolName}' was denied {rule.Kind.ToString().ToLowerInvariant()} access to '{requestedPath}'. Allowed roots: {allowedRoots}.");
    }

    private static PermissionEvaluationResult EvaluateShellPolicy(
        ToolExecutionContext context,
        ShellCommandPermissionPolicy shellPolicy)
    {
        if (!TryGetOptionalString(context.Arguments, shellPolicy.CommandArgumentName, out string? commandText))
        {
            return PermissionEvaluationResult.Allowed();
        }

        string? commandName = ExtractCommandName(commandText!);
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return PermissionEvaluationResult.Denied(
                "invalid_shell_command",
                $"Tool '{context.ToolName}' did not receive a valid shell command.");
        }

        bool isAllowed = shellPolicy.AllowedCommands.Contains(
            commandName,
            StringComparer.OrdinalIgnoreCase);

        if (isAllowed)
        {
            return PermissionEvaluationResult.Allowed();
        }

        string allowedCommands = string.Join(", ", shellPolicy.AllowedCommands);
        return PermissionEvaluationResult.Denied(
            "shell_command_not_allowed",
            $"Tool '{context.ToolName}' cannot execute shell command '{commandName}'. Allowed commands: {allowedCommands}.");
    }

    private static string? ExtractCommandName(string commandText)
    {
        ReadOnlySpan<char> value = commandText.AsSpan().TrimStart();
        if (value.IsEmpty)
        {
            return null;
        }

        char firstCharacter = value[0];
        if (firstCharacter is '"' or '\'')
        {
            int closingQuoteIndex = value[1..].IndexOf(firstCharacter);
            if (closingQuoteIndex < 0)
            {
                return null;
            }

            return NormalizeCommandToken(value.Slice(1, closingQuoteIndex).ToString());
        }

        int separatorIndex = value.IndexOfAny(" \t\r\n".AsSpan());
        string token = separatorIndex < 0
            ? value.ToString()
            : value[..separatorIndex].ToString();

        return NormalizeCommandToken(token);
    }

    private static string NormalizeCommandToken(string token)
    {
        string trimmedToken = token.Trim();
        if (string.IsNullOrWhiteSpace(trimmedToken))
        {
            return string.Empty;
        }

        string fileName = Path.GetFileName(trimmedToken.Replace('/', Path.DirectorySeparatorChar));
        return Path.GetFileNameWithoutExtension(fileName);
    }

    private static bool TryGetOptionalString(
        JsonElement arguments,
        string propertyName,
        out string? value)
    {
        if (arguments.TryGetProperty(propertyName, out JsonElement property) &&
            property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString()?.Trim();
            return !string.IsNullOrWhiteSpace(value);
        }

        value = null;
        return false;
    }

    private static string ResolveWithinWorkspace(
        string workspaceRoot,
        string requestedPath)
    {
        string fullPath = Path.GetFullPath(
            Path.IsPathRooted(requestedPath)
                ? requestedPath
                : Path.Combine(workspaceRoot, requestedPath));

        if (!IsSamePathOrDescendant(workspaceRoot, fullPath))
        {
            throw new InvalidOperationException("The requested path is outside the workspace.");
        }

        return fullPath;
    }

    private static bool IsSamePathOrDescendant(
        string parentPath,
        string candidatePath)
    {
        string normalizedParent = EnsureTrailingSeparator(Path.GetFullPath(parentPath));
        string normalizedCandidate = EnsureTrailingSeparator(Path.GetFullPath(candidatePath));

        return normalizedCandidate.StartsWith(
                   normalizedParent,
                   GetPathComparison()) ||
               string.Equals(
                   Path.GetFullPath(parentPath),
                   Path.GetFullPath(candidatePath),
                   GetPathComparison());
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
