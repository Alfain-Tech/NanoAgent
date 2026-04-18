using FinalAgent.Application.Abstractions;
using FinalAgent.ConsoleHost.Logging;
using Microsoft.Extensions.Logging;

namespace FinalAgent.ConsoleHost.Output;

internal sealed class LoggingGreetingSink : IGreetingSink
{
    private readonly ILogger<LoggingGreetingSink> _logger;

    public LoggingGreetingSink(ILogger<LoggingGreetingSink> logger)
    {
        _logger = logger;
    }

    public ValueTask WriteAsync(string message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OutputLogMessages.GreetingWritten(_logger, message);
        return ValueTask.CompletedTask;
    }
}
