using System.Text.Json;
using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;
using NanoAgent.Application.Tools.Serialization;

namespace NanoAgent.Application.Tools;

internal sealed class DirectoryListTool : ITool
{
    private readonly IWorkspaceFileService _workspaceFileService;

    public DirectoryListTool(IWorkspaceFileService workspaceFileService)
    {
        _workspaceFileService = workspaceFileService;
    }

    public string Description => "List files and directories from the current workspace.";

    public string Name => AgentToolNames.DirectoryList;

    public string PermissionRequirements => """
        {
          "approvalMode": "Automatic",
          "filePaths": [
            {
              "argumentName": "path",
              "kind": "List",
              "allowedRoots": ["."]
            }
          ]
        }
        """;

    public string Schema => """
        {
          "type": "object",
          "properties": {
            "path": {
              "type": "string",
              "description": "Directory path relative to the workspace root. Defaults to the workspace root."
            },
            "recursive": {
              "type": "boolean",
              "description": "Whether to include nested files and directories."
            }
          },
          "additionalProperties": false
        }
        """;

    public async Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        string? path = TryGetOptionalString(context.Arguments, "path");
        bool recursive = TryGetOptionalBoolean(context.Arguments, "recursive", out bool recursiveValue) &&
                         recursiveValue;

        Application.Tools.Models.WorkspaceDirectoryListResult result = await _workspaceFileService.ListDirectoryAsync(
            path,
            recursive,
            cancellationToken);

        string[] entryLines = result.Entries
            .Select(entry => $"{entry.EntryType}: {entry.Path}")
            .ToArray();

        string renderText = entryLines.Length == 0
            ? "(empty)"
            : string.Join(Environment.NewLine, entryLines);

        return ToolResultFactory.Success(
            $"Listed directory '{result.Path}'.",
            result,
            ToolJsonContext.Default.WorkspaceDirectoryListResult,
            new ToolRenderPayload(
                $"Directory listing: {result.Path}",
                renderText));
    }

    private static bool TryGetOptionalBoolean(
        JsonElement arguments,
        string propertyName,
        out bool value)
    {
        if (arguments.TryGetProperty(propertyName, out JsonElement property) &&
            property.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            value = property.GetBoolean();
            return true;
        }

        value = default;
        return false;
    }

    private static string? TryGetOptionalString(
        JsonElement arguments,
        string propertyName)
    {
        if (arguments.TryGetProperty(propertyName, out JsonElement property) &&
            property.ValueKind == JsonValueKind.String)
        {
            return property.GetString()?.Trim();
        }

        return null;
    }
}
