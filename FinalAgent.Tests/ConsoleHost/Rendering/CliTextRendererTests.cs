using FinalAgent.ConsoleHost.Rendering;
using FluentAssertions;

namespace FinalAgent.Tests.ConsoleHost.Rendering;

public sealed class CliTextRendererTests
{
    [Fact]
    public async Task RenderAsync_Should_RenderAssistantOutputWithDistinctStyles_When_MessageContainsCodeAndDiff()
    {
        MarkdownLikeCliMessageFormatter formatter = new();
        RecordingCliOutputTarget outputTarget = new();
        CliTextRenderer sut = new(outputTarget);

        CliRenderDocument document = formatter.Format(
            CliRenderMessageKind.Assistant,
            """
            ## Review
            Use `dotnet test`.

            ```diff
            + added
            - removed
            ```
            """);

        await sut.RenderAsync(document, CancellationToken.None);

        outputTarget.Lines[0].Should().ContainSingle();
        outputTarget.Lines[0][0].Text.Should().Be("assistant");
        outputTarget.Lines[0][0].Style.Should().Be(CliOutputStyle.AssistantLabel);

        outputTarget.Lines.SelectMany(static line => line).Should().Contain(segment =>
            segment.Text.Contains("dotnet test", StringComparison.Ordinal) &&
            segment.Style == CliOutputStyle.InlineCode);

        outputTarget.Lines.SelectMany(static line => line).Should().Contain(segment =>
            segment.Text.Contains("+ added", StringComparison.Ordinal) &&
            segment.Style == CliOutputStyle.DiffAddition);

        outputTarget.Lines.SelectMany(static line => line).Should().Contain(segment =>
            segment.Text.Contains("- removed", StringComparison.Ordinal) &&
            segment.Style == CliOutputStyle.DiffRemoval);
    }

    [Fact]
    public async Task RenderAsync_Should_RenderWarningAndErrorPrefixes_When_DocumentIsStatusMessage()
    {
        MarkdownLikeCliMessageFormatter formatter = new();
        RecordingCliOutputTarget outputTarget = new();
        CliTextRenderer sut = new(outputTarget);

        await sut.RenderAsync(
            formatter.Format(CliRenderMessageKind.Warning, "Check the generated patch."),
            CancellationToken.None);

        await sut.RenderAsync(
            formatter.Format(CliRenderMessageKind.Error, "The provider request failed."),
            CancellationToken.None);

        outputTarget.Lines[0][0].Text.Should().Be("[warning] ");
        outputTarget.Lines[0][0].Style.Should().Be(CliOutputStyle.Warning);

        outputTarget.Lines[1][0].Text.Should().Be("[error] ");
        outputTarget.Lines[1][0].Style.Should().Be(CliOutputStyle.Error);
    }

    private sealed class RecordingCliOutputTarget : ICliOutputTarget
    {
        public bool SupportsColor => true;

        public List<IReadOnlyList<CliOutputSegment>> Lines { get; } = [];

        public void WriteLine()
        {
            Lines.Add([]);
        }

        public void WriteLine(IReadOnlyList<CliOutputSegment> segments)
        {
            Lines.Add(segments.ToArray());
        }
    }
}
