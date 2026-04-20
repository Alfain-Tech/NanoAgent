using System.Text.Json;
using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;
using NanoAgent.Application.Tools.Models;
using NanoAgent.Application.Tools.Serialization;

namespace NanoAgent.Application.Tools;

internal sealed class SearchFilesTool : ITool
{
    private readonly IWorkspaceFileService _workspaceFileService;

    public SearchFilesTool(IWorkspaceFileService workspaceFileService)
    {
        _workspaceFileService = workspaceFileService;
    }

    public string Description => "Search for files in the current workspace by name or relative path fragment.";

    public string Name => AgentToolNames.SearchFiles;

    public string PermissionRequirements => """
        {
          "approvalMode": "Automatic",
          "filePaths": [
            {
              "argumentName": "path",
              "kind": "Search",
              "allowedRoots": ["."]
            }
          ]
        }
        """;

    public string Schema => """
        {
          "type": "object",
          "properties": {
            "query": {
              "type": "string",
              "description": "File name or relative path text to search for."
            },
            "path": {
              "type": "string",
              "description": "Optional file or directory path relative to the workspace root."
            },
            "caseSensitive": {
              "type": "boolean",
              "description": "Whether to use case-sensitive matching."
            }
          },
          "required": ["query"],
          "additionalProperties": false
        }
        """;

    public async Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetRequiredString(context.Arguments, "query", out string? query))
        {
            return ToolResultFactory.InvalidArguments(
                "missing_query",
                "Tool 'search_files' requires a non-empty 'query' string.",
                new ToolRenderPayload(
                    "Invalid search_files arguments",
                    "Provide a non-empty 'query' string."));
        }

        WorkspaceFileSearchResult result = await _workspaceFileService.SearchFilesAsync(
            new WorkspaceFileSearchRequest(
                query!,
                TryGetOptionalString(context.Arguments, "path"),
                TryGetOptionalBoolean(context.Arguments, "caseSensitive", out bool caseSensitive) && caseSensitive),
            cancellationToken);

        string renderText = result.Matches.Count == 0
            ? "No matching files found."
            : string.Join(Environment.NewLine, result.Matches);

        return ToolResultFactory.Success(
            $"Found {result.Matches.Count} matching {(result.Matches.Count == 1 ? "file" : "files")} for '{result.Query}'.",
            result,
            ToolJsonContext.Default.WorkspaceFileSearchResult,
            new ToolRenderPayload(
                $"File search for '{result.Query}'",
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

    private static bool TryGetRequiredString(
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
}
