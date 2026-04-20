using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;

namespace NanoAgent.Application.Repl.Commands;

internal sealed class DenyCommandHandler : IReplCommandHandler
{
    public string CommandName => "deny";

    public string Description => "Add a session-scoped deny override for a tool/tag and optional target pattern.";

    public string Usage => "/deny <tool-or-tag> [pattern]";

    public Task<ReplCommandResult> ExecuteAsync(
        ReplCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (!PermissionCommandSupport.TryParseOverrideArguments(
                context,
                Usage,
                out string toolPattern,
                out string? subjectPattern,
                out ReplCommandResult? errorResult))
        {
            return Task.FromResult(errorResult!);
        }

        context.Session.AddPermissionOverride(new PermissionRule
        {
            Mode = PermissionMode.Deny,
            Tools = [toolPattern],
            Patterns = subjectPattern is null ? [] : [subjectPattern]
        });

        string message = subjectPattern is null
            ? $"Added a session deny rule for '{toolPattern}' across all targets. Use /rules to review it."
            : $"Added a session deny rule for '{toolPattern}' on '{subjectPattern}'. Use /rules to review it.";

        return Task.FromResult(ReplCommandResult.Continue(message, ReplFeedbackKind.Info));
    }
}
