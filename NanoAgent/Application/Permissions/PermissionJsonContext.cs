using System.Text.Json.Serialization;
using NanoAgent.Application.Models;

namespace NanoAgent.Application.Permissions;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(ToolPermissionPolicy))]
[JsonSerializable(typeof(FilePathPermissionRule))]
[JsonSerializable(typeof(ShellCommandPermissionPolicy))]
internal sealed partial class PermissionJsonContext : JsonSerializerContext
{
}
