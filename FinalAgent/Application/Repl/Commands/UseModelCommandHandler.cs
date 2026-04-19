using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;

namespace FinalAgent.Application.Repl.Commands;

internal sealed class UseModelCommandHandler : IReplCommandHandler
{
    public string CommandName => "use";

    public string Description => "Switch the active model for subsequent prompts.";

    public string Usage => "/use <model>";

    public Task<ReplCommandResult> ExecuteAsync(
        ReplCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(context.ArgumentText))
        {
            return Task.FromResult(ReplCommandResult.Continue(
                "Usage: /use <model>",
                ReplFeedbackKind.Error));
        }

        string requestedModel = context.ArgumentText.Trim();
        string[] exactMatches = context.Session.AvailableModelIds
            .Where(modelId => string.Equals(modelId, requestedModel, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (exactMatches.Length == 1)
        {
            return Task.FromResult(SwitchModel(context.Session, exactMatches[0]));
        }

        string[] suffixMatches = context.Session.AvailableModelIds
            .Where(modelId => HasMatchingTerminalSegment(modelId, requestedModel))
            .ToArray();

        if (suffixMatches.Length == 1)
        {
            return Task.FromResult(SwitchModel(context.Session, suffixMatches[0]));
        }

        if (suffixMatches.Length > 1)
        {
            return Task.FromResult(ReplCommandResult.Continue(
                "Model name is ambiguous. Matches: " + string.Join(", ", suffixMatches),
                ReplFeedbackKind.Error));
        }

        return Task.FromResult(ReplCommandResult.Continue(
            $"Model '{requestedModel}' is not available. Use /models to see valid choices.",
            ReplFeedbackKind.Error));
    }

    private static bool HasMatchingTerminalSegment(string modelId, string requestedModel)
    {
        int lastSlashIndex = modelId.LastIndexOf('/');
        if (lastSlashIndex < 0 || lastSlashIndex == modelId.Length - 1)
        {
            return false;
        }

        return string.Equals(
            modelId[(lastSlashIndex + 1)..],
            requestedModel,
            StringComparison.OrdinalIgnoreCase);
    }

    private static ReplCommandResult SwitchModel(
        ReplSessionContext session,
        string resolvedModelId)
    {
        if (string.Equals(session.ActiveModelId, resolvedModelId, StringComparison.Ordinal))
        {
            return ReplCommandResult.Continue(
                $"Already using '{resolvedModelId}'.");
        }

        session.SetActiveModel(resolvedModelId);

        return ReplCommandResult.Continue(
            $"Active model switched to '{resolvedModelId}'.");
    }
}
