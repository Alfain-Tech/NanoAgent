using NanoAgent.Application.Abstractions;

namespace NanoAgent.Infrastructure.Tools;

internal sealed class CurrentDirectoryWorkspaceRootProvider : IWorkspaceRootProvider
{
    public string GetWorkspaceRoot()
    {
        return Directory.GetCurrentDirectory();
    }
}
