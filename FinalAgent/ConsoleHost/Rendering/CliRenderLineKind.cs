namespace FinalAgent.ConsoleHost.Rendering;

internal enum CliRenderLineKind
{
    Normal = 0,
    DiffAddition = 1,
    DiffRemoval = 2,
    DiffHeader = 3,
    DiffContext = 4
}
