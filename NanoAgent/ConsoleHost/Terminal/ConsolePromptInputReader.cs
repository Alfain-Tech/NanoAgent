using System.Text;
using NanoAgent.Application.Exceptions;

namespace NanoAgent.ConsoleHost.Terminal;

internal sealed class ConsolePromptInputReader : IConsolePromptInputReader
{
    private readonly IConsoleTerminal _terminal;

    public ConsolePromptInputReader(IConsoleTerminal terminal)
    {
        _terminal = terminal;
    }

    public Task<string> ReadLineAsync(
        string? defaultValue,
        ConsoleInputEchoMode echoMode,
        bool allowCancellation,
        CancellationToken cancellationToken)
    {
        StringBuilder builder = new();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ConsoleKeyInfo keyInfo = _terminal.ReadKey(intercept: true);
            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    _terminal.WriteLine();
                    if (builder.Length == 0 && defaultValue is not null)
                    {
                        return Task.FromResult(defaultValue);
                    }

                    return Task.FromResult(builder.ToString());

                case ConsoleKey.Backspace:
                    if (builder.Length > 0)
                    {
                        builder.Length--;
                        _terminal.Write("\b \b");
                    }

                    break;

                case ConsoleKey.Escape when allowCancellation:
                    _terminal.WriteLine();
                    throw new PromptCancelledException();

                default:
                    if (!char.IsControl(keyInfo.KeyChar))
                    {
                        builder.Append(keyInfo.KeyChar);
                        _terminal.Write(echoMode == ConsoleInputEchoMode.SecretMask
                            ? "*"
                            : keyInfo.KeyChar.ToString());
                    }

                    break;
            }
        }
    }
}
