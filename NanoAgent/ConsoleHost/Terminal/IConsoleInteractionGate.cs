namespace NanoAgent.ConsoleHost.Terminal;

internal interface IConsoleInteractionGate
{
    IDisposable EnterScope();
}
