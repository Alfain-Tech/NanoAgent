using NanoAgent.Application.Abstractions;
using NanoAgent.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace NanoAgent.ConsoleHost.Repl;

internal sealed class ConsoleReplInputReader : IReplInputReader
{
    private readonly Terminal.IConsoleTerminal _terminal;
    private readonly string _prompt;

    public ConsoleReplInputReader(
        Terminal.IConsoleTerminal terminal,
        IOptions<ApplicationOptions> options)
    {
        _terminal = terminal;
        _prompt = BuildPrompt(options.Value.ProductName);
    }

    public Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_terminal.IsOutputRedirected)
        {
            _terminal.Write(_prompt);
        }

        return Task.FromResult(_terminal.ReadLine());
    }

    private static string BuildPrompt(string productName)
    {
        string normalizedName = string.IsNullOrWhiteSpace(productName)
            ? "agent"
            : productName.Trim().ToLowerInvariant();

        return $"\u001b[38;5;244m{normalizedName}>\u001b[0m ";
    }
}
