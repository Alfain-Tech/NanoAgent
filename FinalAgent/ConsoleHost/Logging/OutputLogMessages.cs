using Microsoft.Extensions.Logging;

namespace FinalAgent.ConsoleHost.Logging;

internal static partial class OutputLogMessages
{
    [LoggerMessage(
        EventId = 2100,
        Level = LogLevel.Information,
        Message = "{message}")]
    public static partial void GreetingWritten(ILogger logger, string message);
}
