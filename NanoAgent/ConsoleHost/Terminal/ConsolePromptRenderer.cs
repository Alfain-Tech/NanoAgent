using NanoAgent.Application.Models;

namespace NanoAgent.ConsoleHost.Terminal;

internal sealed class ConsolePromptRenderer : IConsolePromptRenderer
{
    private readonly IConsoleTerminal _terminal;

    public ConsolePromptRenderer(IConsoleTerminal terminal)
    {
        _terminal = terminal;
    }

    public int WriteInteractiveSelectionPrompt<T>(SelectionPromptRequest<T> request, int selectedIndex)
    {
        WriteHeading(request.Title, request.Description);
        _terminal.WriteLine(BuildInteractiveInstructions(request.AllowCancellation));
        _terminal.WriteLine();

        int optionsTop = _terminal.CursorTop;
        WriteSelectionOptions(request.Options, selectedIndex);
        return optionsTop;
    }

    public void RewriteSelectionOptions<T>(
        SelectionPromptRequest<T> request,
        int selectedIndex,
        int optionsTop)
    {
        _terminal.SetCursorPosition(0, optionsTop);
        WriteSelectionOptions(request.Options, selectedIndex);
        _terminal.SetCursorPosition(0, optionsTop + request.Options.Count);
    }

    public void WriteFallbackSelectionPrompt<T>(SelectionPromptRequest<T> request)
    {
        WriteHeading(request.Title, request.Description);

        for (int index = 0; index < request.Options.Count; index++)
        {
            SelectionPromptOption<T> option = request.Options[index];
            _terminal.WriteLine($"{index + 1}. {BuildOptionLabel(option)}");
        }

        _terminal.WriteLine();
    }

    public void WriteSecretPrompt(SecretPromptRequest request)
    {
        WriteHeading(request.Label, request.Description);
        _terminal.Write("> ");
    }

    public void WriteStatus(StatusMessageKind kind, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        string prefix = kind switch
        {
            StatusMessageKind.Error => "[error]",
            StatusMessageKind.Success => "[ok]",
            _ => "[info]"
        };

        if (_terminal.IsOutputRedirected)
        {
            _terminal.WriteLine($"{prefix} {message}");
            return;
        }

        (ConsoleColor foreground, ConsoleColor background) = kind switch
        {
            StatusMessageKind.Error => (ConsoleColor.White, ConsoleColor.DarkRed),
            StatusMessageKind.Success => (ConsoleColor.Black, ConsoleColor.DarkGreen),
            _ => (ConsoleColor.Black, ConsoleColor.DarkCyan)
        };

        WriteStyledLine($"{prefix} {message}", foreground, background);
    }

    public void WriteTextPrompt(TextPromptRequest request)
    {
        WriteHeading(request.Label, request.Description);
        if (!string.IsNullOrWhiteSpace(request.DefaultValue))
        {
            _terminal.WriteLine($"Default: {request.DefaultValue}");
        }

        _terminal.Write("> ");
    }

    private string BuildOptionLabel<T>(SelectionPromptOption<T> option)
    {
        return string.IsNullOrWhiteSpace(option.Description)
            ? option.Label
            : $"{option.Label} - {option.Description}";
    }

    private string BuildInteractiveInstructions(bool allowCancellation)
    {
        return allowCancellation
            ? "Use Up/Down to move, Enter to confirm, Esc to cancel."
            : "Use Up/Down to move and Enter to confirm.";
    }

    private string FormatInteractiveOption<T>(SelectionPromptOption<T> option, bool isSelected)
    {
        string prefix = isSelected ? "> " : "  ";
        return prefix + BuildOptionLabel(option);
    }

    private int GetLineWidth()
    {
        return _terminal.WindowWidth > 0 ? _terminal.WindowWidth : 80;
    }

    private string PadLine(string value)
    {
        int width = Math.Max(1, GetLineWidth() - 1);
        string trimmed = value.Length > width
            ? value[..Math.Max(0, width - 3)] + "..."
            : value;

        return trimmed.PadRight(width);
    }

    private void WriteHeading(string title, string? description)
    {
        _terminal.WriteLine(title);
        if (!string.IsNullOrWhiteSpace(description))
        {
            _terminal.WriteLine(description);
        }
    }

    private void WriteSelectionOptions<T>(
        IReadOnlyList<SelectionPromptOption<T>> options,
        int selectedIndex)
    {
        for (int index = 0; index < options.Count; index++)
        {
            bool isSelected = index == selectedIndex;
            string line = PadLine(FormatInteractiveOption(options[index], isSelected));

            if (isSelected)
            {
                WriteStyledLine(line, ConsoleColor.Black, ConsoleColor.Gray);
            }
            else
            {
                _terminal.WriteLine(line);
            }
        }
    }

    private void WriteStyledLine(string text, ConsoleColor foreground, ConsoleColor background)
    {
        ConsoleColor originalForeground = _terminal.ForegroundColor;
        ConsoleColor originalBackground = _terminal.BackgroundColor;

        try
        {
            _terminal.ForegroundColor = foreground;
            _terminal.BackgroundColor = background;
            _terminal.WriteLine(text);
        }
        finally
        {
            _terminal.ForegroundColor = originalForeground;
            _terminal.BackgroundColor = originalBackground;
        }
    }
}
