using System.Text.Json.Serialization;
using FinalAgent.Application.Tools.Models;

namespace FinalAgent.Application.Tools.Serialization;

[JsonSerializable(typeof(ToolErrorPayload))]
[JsonSerializable(typeof(WorkspaceFileReadResult))]
[JsonSerializable(typeof(WorkspaceFileWriteResult))]
[JsonSerializable(typeof(WorkspaceDirectoryListResult))]
[JsonSerializable(typeof(WorkspaceDirectoryEntry))]
[JsonSerializable(typeof(WorkspaceTextSearchResult))]
[JsonSerializable(typeof(WorkspaceTextSearchMatch))]
[JsonSerializable(typeof(ShellCommandExecutionResult))]
internal sealed partial class ToolJsonContext : JsonSerializerContext
{
}
