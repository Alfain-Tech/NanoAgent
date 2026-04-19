using FinalAgent.Application.Models;

namespace FinalAgent.Application.Abstractions;

public interface IPermissionEvaluator
{
    PermissionEvaluationResult Evaluate(
        ToolPermissionPolicy permissionPolicy,
        PermissionEvaluationContext context);
}
