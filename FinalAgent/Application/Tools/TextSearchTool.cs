using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Tools.Models;
using FinalAgent.Application.Tools.Serialization;

namespace FinalAgent.Application.Tools;

internal sealed class TextSearchTool : ITool
{
    private readonly IWorkspaceFileService _workspaceFileService;

    public TextSearchTool(IWorkspaceFileService workspaceFileService)
    {
        _workspaceFileService = workspaceFileService;
    }

    public string Description => "Search text recursively within files in the current workspace.";

    public string Name => AgentToolNames.TextSearch;

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
              "description": "Text to search for."
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
                "Tool 'text_search' requires a non-empty 'query' string.",
                new ToolRenderPayload(
                    "Invalid text_search arguments",
                    "Provide a non-empty 'query' string."));
        }

        string safeQuery = query!;

        WorkspaceTextSearchResult result = await _workspaceFileService.SearchTextAsync(
            new WorkspaceTextSearchRequest(
                safeQuery,
                TryGetOptionalString(context.Arguments, "path"),
                TryGetOptionalBoolean(context.Arguments, "caseSensitive", out bool caseSensitive) && caseSensitive),
            cancellationToken);

        string renderText = result.Matches.Count == 0
            ? "No matches found."
            : string.Join(
                Environment.NewLine,
                result.Matches.Select(match => $"{match.Path}:{match.LineNumber}: {match.LineText}"));

        return ToolResultFactory.Success(
            $"Searched for '{result.Query}' in '{result.Path}'.",
            result,
            ToolJsonContext.Default.WorkspaceTextSearchResult,
            new ToolRenderPayload(
                $"Search results for '{result.Query}'",
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
