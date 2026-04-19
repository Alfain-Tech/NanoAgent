namespace NanoAgent.Application.Models;

public sealed class PermissionEvaluationResult
{
    private PermissionEvaluationResult(
        PermissionEvaluationDecision decision,
        string? reasonCode = null,
        string? reason = null)
    {
        if (decision != PermissionEvaluationDecision.Allowed)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
            ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        }

        Decision = decision;
        ReasonCode = reasonCode?.Trim();
        Reason = reason?.Trim();
    }

    public PermissionEvaluationDecision Decision { get; }

    public bool IsAllowed => Decision == PermissionEvaluationDecision.Allowed;

    public string? Reason { get; }

    public string? ReasonCode { get; }

    public static PermissionEvaluationResult Allowed()
    {
        return new PermissionEvaluationResult(PermissionEvaluationDecision.Allowed);
    }

    public static PermissionEvaluationResult Denied(
        string reasonCode,
        string reason)
    {
        return new PermissionEvaluationResult(
            PermissionEvaluationDecision.Denied,
            reasonCode,
            reason);
    }

    public static PermissionEvaluationResult RequiresApproval(
        string reasonCode,
        string reason)
    {
        return new PermissionEvaluationResult(
            PermissionEvaluationDecision.RequiresApproval,
            reasonCode,
            reason);
    }
}
