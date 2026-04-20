using NanoAgent.Application.Models;

namespace NanoAgent.Application.Abstractions;

public interface IPermissionApprovalPrompt
{
    Task<PermissionApprovalChoice> PromptAsync(
        PermissionApprovalRequest request,
        CancellationToken cancellationToken);
}
