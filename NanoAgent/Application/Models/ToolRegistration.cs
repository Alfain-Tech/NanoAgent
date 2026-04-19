using NanoAgent.Application.Abstractions;

namespace NanoAgent.Application.Models;

public sealed class ToolRegistration
{
    public ToolRegistration(
        ITool tool,
        ToolDefinition definition,
        ToolPermissionPolicy permissionPolicy)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(permissionPolicy);

        Tool = tool;
        Definition = definition;
        PermissionPolicy = permissionPolicy;
    }

    public ToolDefinition Definition { get; }

    public string Name => Tool.Name;

    public ToolPermissionPolicy PermissionPolicy { get; }

    public ITool Tool { get; }
}
