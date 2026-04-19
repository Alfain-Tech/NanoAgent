using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Tools.Serialization;

namespace FinalAgent.Application.Tools;

internal sealed class FileReadTool : ITool
{
    private readonly IWorkspaceFileService _workspaceFileService;

    public FileReadTool(IWorkspaceFileService workspaceFileService)
    {
        _workspaceFileService = workspaceFileService;
    }

    public string Description => "Read a UTF-8 text file from the current workspace.";

    public string Name => AgentToolNames.FileRead;

    public string Schema => """
        {
          "type": "object",
          "properties": {
            "path": {
              "type": "string",
              "description": "Path to the file, relative to the workspace root."
            }
          },
          "required": ["path"],
          "additionalProperties": false
        }
        """;

    public async Task<ToolResult> ExecuteAsync(
        ToolExecutionContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryGetRequiredString(context.Arguments, "path", out string? path))
        {
            return ToolResultFactory.InvalidArguments(
                "missing_path",
                "Tool 'file_read' requires a non-empty 'path' string.",
                new ToolRenderPayload(
                    "Invalid file_read arguments",
                    "Provide a non-empty 'path' string."));
        }

        string safePath = path!;

        Application.Tools.Models.WorkspaceFileReadResult result = await _workspaceFileService.ReadFileAsync(
            safePath,
            cancellationToken);

        return ToolResultFactory.Success(
            $"Read file '{result.Path}'.",
            result,
            ToolJsonContext.Default.WorkspaceFileReadResult,
            new ToolRenderPayload(
                $"File: {result.Path}",
                result.Content));
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
