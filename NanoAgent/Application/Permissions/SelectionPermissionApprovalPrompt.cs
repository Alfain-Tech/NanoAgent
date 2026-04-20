using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;

namespace NanoAgent.Application.Permissions;

internal sealed class SelectionPermissionApprovalPrompt : IPermissionApprovalPrompt
{
    private readonly ISelectionPrompt _selectionPrompt;

    public SelectionPermissionApprovalPrompt(ISelectionPrompt selectionPrompt)
    {
        _selectionPrompt = selectionPrompt;
    }

    public Task<PermissionApprovalChoice> PromptAsync(
        PermissionApprovalRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string subjectLabel = request.Request.ToolKind switch
        {
            "bash" => "Command",
            "webfetch" => "Request",
            "read" or "edit" or "external_directory" => "Path",
            _ => "Target"
        };

        string subjectText = request.Request.Subjects.Count == 0
            ? $"{subjectLabel}: (tool-wide)"
            : string.Join(
                Environment.NewLine,
                request.Request.Subjects.Select(subject => $"{subjectLabel}: {subject}"));

        SelectionPromptRequest<PermissionApprovalChoice> selectionRequest = new(
            $"Approve {request.Request.ToolKind} access?",
            [
                new SelectionPromptOption<PermissionApprovalChoice>(
                    "Allow once",
                    PermissionApprovalChoice.AllowOnce,
                    "Run this request now without saving an override."),
                new SelectionPromptOption<PermissionApprovalChoice>(
                    $"Allow for {request.AgentName}",
                    PermissionApprovalChoice.AllowForAgent,
                    "Remember an allow override for this exact pattern on the current agent."),
                new SelectionPromptOption<PermissionApprovalChoice>(
                    "Deny once",
                    PermissionApprovalChoice.DenyOnce,
                    "Block this request now but keep prompting in the future."),
                new SelectionPromptOption<PermissionApprovalChoice>(
                    $"Deny for {request.AgentName}",
                    PermissionApprovalChoice.DenyForAgent,
                    "Remember a deny override for this exact pattern on the current agent.")
            ],
            $"{request.Reason}{Environment.NewLine}{Environment.NewLine}Tool: {request.Request.ToolName}{Environment.NewLine}{subjectText}",
            DefaultIndex: 2,
            AllowCancellation: true);

        return _selectionPrompt.PromptAsync(selectionRequest, cancellationToken);
    }
}
