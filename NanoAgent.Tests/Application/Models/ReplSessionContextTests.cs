using NanoAgent.Application.Models;
using NanoAgent.Domain.Models;
using FluentAssertions;

namespace NanoAgent.Tests.Application.Models;

public sealed class ReplSessionContextTests
{
    [Fact]
    public void RecordFileEditTransaction_Should_ExposePendingUndo_And_CompleteUndoShouldMoveItToRedo()
    {
        ReplSessionContext session = CreateSession();
        WorkspaceFileEditTransaction transaction = CreateTransaction("file_write (README.md)");

        session.RecordFileEditTransaction(transaction);

        session.TryGetPendingUndoFileEdit(out WorkspaceFileEditTransaction? pendingUndo).Should().BeTrue();
        pendingUndo.Should().BeSameAs(transaction);

        session.CompleteUndoFileEdit();

        session.TryGetPendingUndoFileEdit(out _).Should().BeFalse();
        session.TryGetPendingRedoFileEdit(out WorkspaceFileEditTransaction? pendingRedo).Should().BeTrue();
        pendingRedo.Should().BeSameAs(transaction);
    }

    [Fact]
    public void CompleteRedoFileEdit_Should_MoveTransactionBackToUndoStack()
    {
        ReplSessionContext session = CreateSession();
        WorkspaceFileEditTransaction transaction = CreateTransaction("apply_patch (2 files)");
        session.RecordFileEditTransaction(transaction);
        session.CompleteUndoFileEdit();

        session.CompleteRedoFileEdit();

        session.TryGetPendingRedoFileEdit(out _).Should().BeFalse();
        session.TryGetPendingUndoFileEdit(out WorkspaceFileEditTransaction? pendingUndo).Should().BeTrue();
        pendingUndo.Should().BeSameAs(transaction);
    }

    [Fact]
    public void RecordFileEditTransaction_Should_ClearRedoStack_When_NewEditArrivesAfterUndo()
    {
        ReplSessionContext session = CreateSession();
        session.RecordFileEditTransaction(CreateTransaction("file_write (README.md)"));
        session.CompleteUndoFileEdit();

        session.RecordFileEditTransaction(CreateTransaction("apply_patch (2 files)"));

        session.TryGetPendingRedoFileEdit(out _).Should().BeFalse();
    }

    private static ReplSessionContext CreateSession()
    {
        return new ReplSessionContext(
            new AgentProviderProfile(ProviderKind.OpenAiCompatible, "https://provider.example.com/v1"),
            "gpt-5-mini",
            ["gpt-5-mini"]);
    }

    private static WorkspaceFileEditTransaction CreateTransaction(string description)
    {
        return new WorkspaceFileEditTransaction(
            description,
            [new WorkspaceFileEditState("README.md", exists: false, content: null)],
            [new WorkspaceFileEditState("README.md", exists: true, content: "hello")]);
    }
}
