using System.Text.Json.Serialization;
using FinalAgent.Application.Models;

namespace FinalAgent.Application.Permissions;

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(ToolPermissionPolicy))]
[JsonSerializable(typeof(FilePathPermissionRule))]
[JsonSerializable(typeof(ShellCommandPermissionPolicy))]
internal sealed partial class PermissionJsonContext : JsonSerializerContext
{
}
