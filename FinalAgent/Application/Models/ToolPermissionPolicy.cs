namespace FinalAgent.Application.Models;

public sealed class ToolPermissionPolicy
{
    public ToolApprovalMode ApprovalMode { get; set; } = ToolApprovalMode.Automatic;

    public FilePathPermissionRule[] FilePaths { get; set; } = [];

    public ShellCommandPermissionPolicy? Shell { get; set; }
}
