using NanoAgent.ConsoleHost.Rendering;
using FluentAssertions;

namespace NanoAgent.Tests.ConsoleHost.Rendering;

public sealed class MarkdownLikeCliMessageFormatterTests
{
    [Fact]
    public void Format_Should_CreateStructuredBlocks_When_AssistantMessageContainsMarkdownLikeContent()
    {
        MarkdownLikeCliMessageFormatter sut = new();

        CliRenderDocument document = sut.Format(
            CliRenderMessageKind.Assistant,
            """
            # Plan
            Run `dotnet test` before **shipping**.

            ```csharp
            Console.WriteLine("hello");
            ```

            ```diff
            + added line
            - removed line
            ```
            """);

        document.Kind.Should().Be(CliRenderMessageKind.Assistant);
        document.Blocks.Select(block => block.Kind).Should().Equal(
            CliRenderBlockKind.Heading,
            CliRenderBlockKind.Paragraph,
            CliRenderBlockKind.CodeBlock,
            CliRenderBlockKind.Diff);

        document.Blocks[1].Lines[0].Segments.Should().Contain(segment =>
            segment.Style == CliInlineStyle.Code &&
            segment.Text == "dotnet test");

        document.Blocks[1].Lines[0].Segments.Should().Contain(segment =>
            segment.Style == CliInlineStyle.Strong &&
            segment.Text == "shipping");

        document.Blocks[2].Language.Should().Be("csharp");
        document.Blocks[3].Lines.Select(line => line.Kind).Should().Contain([
            CliRenderLineKind.DiffAddition,
            CliRenderLineKind.DiffRemoval
        ]);
    }

    [Fact]
    public void Format_Should_CreateAlertBlock_When_MessageIsWarning()
    {
        MarkdownLikeCliMessageFormatter sut = new();

        CliRenderDocument document = sut.Format(
            CliRenderMessageKind.Warning,
            "Warning: review the generated diff.");

        document.Blocks.Should().ContainSingle();
        document.Blocks[0].Kind.Should().Be(CliRenderBlockKind.Alert);
        document.Blocks[0].Lines[0].Segments[0].Text.Should().Contain("Warning:");
    }
}
