using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Models;

namespace FinalAgent.Application.Repl.Commands;

internal sealed class UseModelCommandHandler : IReplCommandHandler
{
    private readonly IModelActivationService _modelActivationService;

    public UseModelCommandHandler(IModelActivationService modelActivationService)
    {
        _modelActivationService = modelActivationService;
    }

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
        ModelActivationResult result = _modelActivationService.Resolve(
            context.Session,
            requestedModel);

        return Task.FromResult(result.Status switch
        {
            ModelActivationStatus.Switched =>
                ReplCommandResult.Continue(
                    $"Active model switched to '{result.ResolvedModelId}'."),
            ModelActivationStatus.AlreadyActive =>
                ReplCommandResult.Continue(
                    $"Already using '{result.ResolvedModelId}'."),
            ModelActivationStatus.Ambiguous =>
                ReplCommandResult.Continue(
                    "Model name is ambiguous. Matches: " + string.Join(", ", result.CandidateModelIds),
                    ReplFeedbackKind.Error),
            _ =>
                ReplCommandResult.Continue(
                    $"Model '{requestedModel}' is not available. Use /models to see valid choices.",
                    ReplFeedbackKind.Error)
        });
    }
}
