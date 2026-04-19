namespace FinalAgent.ConsoleHost.Rendering;

internal interface ICliMessageFormatter
{
    CliRenderDocument Format(
        CliRenderMessageKind kind,
        string message);
}
