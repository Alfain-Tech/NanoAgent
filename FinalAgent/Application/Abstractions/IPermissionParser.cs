using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IPermissionParser
{
    ToolPermissionPolicy Parse(
        string toolName,
        string permissionRequirementsJson);
}
