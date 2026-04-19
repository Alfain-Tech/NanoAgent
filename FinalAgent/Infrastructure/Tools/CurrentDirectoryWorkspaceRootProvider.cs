using FinalAgent.Application.Abstractions;

namespace FinalAgent.Infrastructure.Tools;

internal sealed class CurrentDirectoryWorkspaceRootProvider : IWorkspaceRootProvider
{
    public string GetWorkspaceRoot()
    {
        return Directory.GetCurrentDirectory();
    }
}
