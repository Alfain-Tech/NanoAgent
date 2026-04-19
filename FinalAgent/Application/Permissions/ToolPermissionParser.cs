using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;

namespace FinalAgent.Application.Permissions;

internal sealed class ToolPermissionParser : IPermissionParser
{
    public ToolPermissionPolicy Parse(
        string toolName,
        string permissionRequirementsJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionRequirementsJson);

        try
        {
            using JsonDocument document = JsonDocument.Parse(permissionRequirementsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    $"Tool '{toolName}' must provide a JSON-object permission policy.");
            }

            ToolPermissionPolicy? policy = JsonSerializer.Deserialize(
                document.RootElement.GetRawText(),
                PermissionJsonContext.Default.ToolPermissionPolicy);

            if (policy is null)
            {
                throw new InvalidOperationException(
                    $"Tool '{toolName}' produced an empty permission policy.");
            }

            return Normalize(toolName, policy);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Tool '{toolName}' must provide a valid JSON permission policy.",
                exception);
        }
    }

    private static ToolPermissionPolicy Normalize(
        string toolName,
        ToolPermissionPolicy policy)
    {
        FilePathPermissionRule[] filePathRules = (policy.FilePaths ?? [])
            .Select(rule => NormalizeFilePathRule(toolName, rule))
            .ToArray();

        ShellCommandPermissionPolicy? shellPolicy = policy.Shell is null
            ? null
            : NormalizeShellPolicy(toolName, policy.Shell);

        return new ToolPermissionPolicy
        {
            ApprovalMode = policy.ApprovalMode,
            FilePaths = filePathRules,
            Shell = shellPolicy
        };
    }

    private static FilePathPermissionRule NormalizeFilePathRule(
        string toolName,
        FilePathPermissionRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (string.IsNullOrWhiteSpace(rule.ArgumentName))
        {
            throw new InvalidOperationException(
                $"Tool '{toolName}' contains a file-path permission rule without an argument name.");
        }

        string[] allowedRoots = (rule.AllowedRoots ?? [])
            .Where(static root => !string.IsNullOrWhiteSpace(root))
            .Select(static root => root.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (allowedRoots.Length == 0)
        {
            throw new InvalidOperationException(
                $"Tool '{toolName}' must provide at least one allowed root for file-path permission argument '{rule.ArgumentName}'.");
        }

        return new FilePathPermissionRule
        {
            ArgumentName = rule.ArgumentName.Trim(),
            Kind = rule.Kind,
            AllowedRoots = allowedRoots
        };
    }

    private static ShellCommandPermissionPolicy NormalizeShellPolicy(
        string toolName,
        ShellCommandPermissionPolicy shellPolicy)
    {
        ArgumentNullException.ThrowIfNull(shellPolicy);

        if (string.IsNullOrWhiteSpace(shellPolicy.CommandArgumentName))
        {
            throw new InvalidOperationException(
                $"Tool '{toolName}' must provide a non-empty shell command argument name.");
        }

        string[] allowedCommands = (shellPolicy.AllowedCommands ?? [])
            .Where(static command => !string.IsNullOrWhiteSpace(command))
            .Select(static command => command.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (allowedCommands.Length == 0)
        {
            throw new InvalidOperationException(
                $"Tool '{toolName}' must provide at least one allowed shell command.");
        }

        return new ShellCommandPermissionPolicy
        {
            CommandArgumentName = shellPolicy.CommandArgumentName.Trim(),
            AllowedCommands = allowedCommands
        };
    }
}
