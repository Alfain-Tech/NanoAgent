using FinalAgent.Application.Tools.Models;

namespace FinalAgent.Application.Abstractions;

public interface IWorkspaceFileService
{
    Task<WorkspaceDirectoryListResult> ListDirectoryAsync(
        string? path,
        bool recursive,
        CancellationToken cancellationToken);

    Task<WorkspaceFileReadResult> ReadFileAsync(
        string path,
        CancellationToken cancellationToken);

    Task<WorkspaceTextSearchResult> SearchTextAsync(
        WorkspaceTextSearchRequest request,
        CancellationToken cancellationToken);

    Task<WorkspaceFileWriteResult> WriteFileAsync(
        string path,
        string content,
        bool overwrite,
        CancellationToken cancellationToken);
}
