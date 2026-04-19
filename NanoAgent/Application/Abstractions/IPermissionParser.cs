using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IPermissionParser
{
    ToolPermissionPolicy Parse(
        string toolName,
        string permissionRequirementsJson);
}
