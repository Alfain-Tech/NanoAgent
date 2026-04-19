using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;
using FinalAgent.Application.Tools.Serialization;

namespace FinalAgent.Application.Tools;

internal sealed class FileWriteTool : ITool
{
    private readonly IWorkspaceFileService _workspaceFileService;

    public FileWriteTool(IWorkspaceFileService workspaceFileService)
    {
        _workspaceFileService = workspaceFileService;
    }

    public string Description => "Write UTF-8 text content to a file in the current workspace.";

    public string Name => AgentToolNames.FileWrite;

    public string Schema => """
        {
          "type": "object",
          "properties": {
            "path": {
              "type": "string",
              "description": "Path to the file, relative to the workspace root."
            },
            "content": {
              "type": "string",
              "description": "Full UTF-8 text content to write."
            },
            "overwrite": {
              "type": "boolean",
              "description": "Whether to overwrite an existing file. Defaults to true."
            }
          },
          "required": ["path", "content"],
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
                "Tool 'file_write' requires a non-empty 'path' string.",
                new ToolRenderPayload(
                    "Invalid file_write arguments",
                    "Provide a non-empty 'path' string."));
        }

        if (!TryGetRequiredString(context.Arguments, "content", out string? content))
        {
            return ToolResultFactory.InvalidArguments(
                "missing_content",
                "Tool 'file_write' requires a 'content' string.",
                new ToolRenderPayload(
                    "Invalid file_write arguments",
                    "Provide a 'content' string to write."));
        }

        string safePath = path!;
        string safeContent = content!;

        bool overwrite = TryGetOptionalBoolean(context.Arguments, "overwrite", out bool overwriteValue)
            ? overwriteValue
            : true;

        Application.Tools.Models.WorkspaceFileWriteResult result = await _workspaceFileService.WriteFileAsync(
            safePath,
            safeContent,
            overwrite,
            cancellationToken);

        string renderText = result.OverwroteExistingFile
            ? $"Updated {result.Path} with {result.CharacterCount} characters."
            : $"Created {result.Path} with {result.CharacterCount} characters.";

        return ToolResultFactory.Success(
            $"Wrote file '{result.Path}'.",
            result,
            ToolJsonContext.Default.WorkspaceFileWriteResult,
            new ToolRenderPayload(
                $"File written: {result.Path}",
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

    private static bool TryGetRequiredString(
        JsonElement arguments,
        string propertyName,
        out string? value)
    {
        if (arguments.TryGetProperty(propertyName, out JsonElement property) &&
            property.ValueKind == JsonValueKind.String)
        {
            value = property.GetString();
            return value is not null;
        }

        value = null;
        return false;
    }
}
