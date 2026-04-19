namespace FinalAgent.ConsoleHost.Rendering;

internal interface ICliTextRenderer
{
    Task RenderAsync(
        CliRenderDocument document,
        CancellationToken cancellationToken);
}
