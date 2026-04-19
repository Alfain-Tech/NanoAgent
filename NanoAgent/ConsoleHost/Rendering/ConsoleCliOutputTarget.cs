using NanoAgent.ConsoleHost.Terminal;

namespace NanoAgent.ConsoleHost.Rendering;

internal sealed class ConsoleCliOutputTarget : ICliOutputTarget
{
    private readonly IConsoleTerminal _terminal;

    public ConsoleCliOutputTarget(IConsoleTerminal terminal)
    {
        _terminal = terminal;
    }

    public bool SupportsColor =>
        !_terminal.IsOutputRedirected &&
        !string.Equals(
            Environment.GetEnvironmentVariable("NO_COLOR"),
            "1",
            StringComparison.Ordinal);

    public void WriteLine()
    {
        _terminal.WriteLine();
    }

    public void WriteLine(IReadOnlyList<CliOutputSegment> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        if (segments.Count == 0)
        {
            _terminal.WriteLine();
            return;
        }

        if (!SupportsColor)
        {
            string plainText = string.Concat(segments.Select(static segment => segment.Text));
            _terminal.WriteLine(plainText);
            return;
        }

        ConsoleColor originalForeground = _terminal.ForegroundColor;
        ConsoleColor originalBackground = _terminal.BackgroundColor;

        try
        {
            foreach (CliOutputSegment segment in segments)
            {
                _terminal.ForegroundColor = MapForeground(segment.Style);
                _terminal.Write(segment.Text);
            }

            _terminal.WriteLine();
        }
        finally
        {
            _terminal.ForegroundColor = originalForeground;
            _terminal.BackgroundColor = originalBackground;
            _terminal.ResetColor();
        }
    }

    private static ConsoleColor MapForeground(CliOutputStyle style)
    {
        return style switch
        {
            CliOutputStyle.AssistantLabel => ConsoleColor.Cyan,
            CliOutputStyle.AssistantText => ConsoleColor.Gray,
            CliOutputStyle.Heading => ConsoleColor.White,
            CliOutputStyle.Strong => ConsoleColor.White,
            CliOutputStyle.Emphasis => ConsoleColor.DarkCyan,
            CliOutputStyle.InlineCode => ConsoleColor.Yellow,
            CliOutputStyle.CodeFence => ConsoleColor.DarkGray,
            CliOutputStyle.CodeText => ConsoleColor.Gray,
            CliOutputStyle.DiffAddition => ConsoleColor.Green,
            CliOutputStyle.DiffRemoval => ConsoleColor.Red,
            CliOutputStyle.DiffHeader => ConsoleColor.Cyan,
            CliOutputStyle.DiffContext => ConsoleColor.DarkGray,
            CliOutputStyle.Warning => ConsoleColor.Yellow,
            CliOutputStyle.Error => ConsoleColor.Red,
            CliOutputStyle.Info => ConsoleColor.Cyan,
            CliOutputStyle.Muted => ConsoleColor.DarkGray,
            _ => ConsoleColor.Gray
        };
    }
}
