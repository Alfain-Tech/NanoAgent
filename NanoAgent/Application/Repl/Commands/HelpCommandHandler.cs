using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Models;

namespace NanoAgent.Application.Repl.Commands;

internal sealed class HelpCommandHandler : IReplCommandHandler
{
    public string CommandName => "help";

    public string Description => "List the available shell commands and their usage.";

    public string Usage => "/help";

    public Task<ReplCommandResult> ExecuteAsync(
        ReplCommandContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const string HelpText =
            "Available commands:\n" +
            "/config - Show the current provider, config path, and active model.\n" +
            "/exit - Exit the interactive shell.\n" +
            "/help - List the available shell commands and their usage.\n" +
            "/models - Show the available models in the current session.\n" +
            "/use <model> - Switch the active model for subsequent prompts.";

        return Task.FromResult(ReplCommandResult.Continue(HelpText));
    }
}
