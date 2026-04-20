using NanoAgent.Application.Models;

namespace NanoAgent.Application.Tools.Models;

public sealed record WorkspaceFileWriteExecutionResult(
    WorkspaceFileWriteResult Result,
    WorkspaceFileEditTransaction EditTransaction);
