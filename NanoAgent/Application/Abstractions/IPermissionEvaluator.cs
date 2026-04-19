using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IPermissionEvaluator
{
    PermissionEvaluationResult Evaluate(
        ToolPermissionPolicy permissionPolicy,
        PermissionEvaluationContext context);
}
