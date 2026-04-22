using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Exceptions;
using NanoAgent.Application.Models;
using NanoAgent.ConsoleHost.Terminal;

namespace NanoAgent.ConsoleHost.Prompts;

internal sealed class ConsoleSelectionPrompt : ISelectionPrompt
{
    private readonly IConsoleInteractionGate _interactionGate;
    private readonly IConsoleTerminal _terminal;
    private readonly IConsolePromptRenderer _renderer;

    public ConsoleSelectionPrompt(
        IConsoleInteractionGate interactionGate,
        IConsoleTerminal terminal,
        IConsolePromptRenderer renderer)
    {
        _interactionGate = interactionGate;
        _terminal = terminal;
        _renderer = renderer;
    }

    public Task<T> PromptAsync<T>(SelectionPromptRequest<T> request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Options.Count == 0)
        {
            throw new ArgumentException("At least one option must be provided.", nameof(request));
        }

        if (request.DefaultIndex < 0 || request.DefaultIndex >= request.Options.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Default index must reference a valid option.");
        }

        if (!SupportsInteractiveSelection())
        {
            return Task.FromException<T>(
                new InvalidOperationException("Selection prompts require an interactive terminal."));
        }

        return Task.FromResult(PromptInteractive(request, cancellationToken));
    }

    private T PromptInteractive<T>(SelectionPromptRequest<T> request, CancellationToken cancellationToken)
    {
        using IDisposable _ = _interactionGate.EnterScope();

        int selectedIndex = request.DefaultIndex;
        InteractiveSelectionPromptLayout layout = _renderer.WriteInteractiveSelectionPrompt(request, selectedIndex);

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ConsoleKeyInfo keyInfo = _terminal.ReadKey(intercept: true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (selectedIndex > 0)
                        {
                            selectedIndex--;
                            _renderer.RewriteSelectionOptions(request, selectedIndex, layout);
                        }

                        break;

                    case ConsoleKey.DownArrow:
                        if (selectedIndex < request.Options.Count - 1)
                        {
                            selectedIndex++;
                            _renderer.RewriteSelectionOptions(request, selectedIndex, layout);
                        }

                        break;

                    case ConsoleKey.Home:
                        if (selectedIndex != 0)
                        {
                            selectedIndex = 0;
                            _renderer.RewriteSelectionOptions(request, selectedIndex, layout);
                        }

                        break;

                    case ConsoleKey.End:
                        if (selectedIndex != request.Options.Count - 1)
                        {
                            selectedIndex = request.Options.Count - 1;
                            _renderer.RewriteSelectionOptions(request, selectedIndex, layout);
                        }

                        break;

                    case ConsoleKey.Enter:
                        return request.Options[selectedIndex].Value;

                    case ConsoleKey.Escape when request.AllowCancellation:
                        throw new PromptCancelledException();
                }
            }
        }
        finally
        {
            _renderer.ClearInteractiveSelectionPrompt(layout);
        }
    }

    private bool SupportsInteractiveSelection()
    {
        return !_terminal.IsInputRedirected && !_terminal.IsOutputRedirected;
    }
}
