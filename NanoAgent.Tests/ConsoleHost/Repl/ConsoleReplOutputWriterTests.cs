using NanoAgent.Application.Models;
using NanoAgent.ConsoleHost.Rendering;
using NanoAgent.ConsoleHost.Repl;
using NanoAgent.Tests.ConsoleHost.TestDoubles;
using FluentAssertions;

namespace NanoAgent.Tests.ConsoleHost.Repl;

public sealed class ConsoleReplOutputWriterTests
{
    [Fact]
    public async Task WriteShellHeaderAsync_Should_RenderBanner_When_ShellStarts()
    {
        FakeConsoleTerminal terminal = new();
        ConsoleCliOutputTarget outputTarget = new(terminal);
        ConsoleReplOutputWriter sut = new(
            new MarkdownLikeCliMessageFormatter(),
            new CliTextRenderer(outputTarget),
            outputTarget,
            terminal);

        await sut.WriteShellHeaderAsync("NanoAgent", "gpt-oss-20b", CancellationToken.None);

        terminal.Output.Should().Contain("NanoAgent");
        terminal.Output.Should().Contain("Model: gpt-oss-20b");
        terminal.Output.Should().Contain("GitHub: github.com/rizwan3d/NanoAgent");
        terminal.Output.Should().Contain("Sponsor: ALFAIN Technologies (PVT) Limited (https://alfain.co/)");
        terminal.Output.Should().Contain("Press Ctrl+C or use /exit to quit.");
        terminal.Output.Should().Contain(new string('\u2500', 53));
    }

    [Fact]
    public async Task WriteResponseAsync_Should_RenderMetricsFooter_When_MetricsAreProvided()
    {
        FakeConsoleTerminal terminal = new();
        ConsoleCliOutputTarget outputTarget = new(terminal);
        ConsoleReplOutputWriter sut = new(
            new MarkdownLikeCliMessageFormatter(),
            new CliTextRenderer(outputTarget),
            outputTarget,
            terminal);

        await sut.WriteResponseAsync(
            "Done.",
            new ConversationTurnMetrics(TimeSpan.FromSeconds(4), 14, 26),
            CancellationToken.None);

        terminal.Output.Should().Contain("assistant");
        terminal.Output.Should().Contain("Done.");
        terminal.Output.Should().Contain("(4s \u00B7 \u2193 26 tokens est.)");
    }

    [Fact]
    public async Task BeginResponseProgressAsync_Should_RenderProgressLine_When_OutputIsInteractive()
    {
        FakeConsoleTerminal terminal = new();
        ConsoleCliOutputTarget outputTarget = new(terminal);
        ConsoleReplOutputWriter sut = new(
            new MarkdownLikeCliMessageFormatter(),
            new CliTextRenderer(outputTarget),
            outputTarget,
            terminal);

        await using IAsyncDisposable progress = await sut.BeginResponseProgressAsync(14, 0, CancellationToken.None);

        terminal.Output.Should().Contain("\u2193 14 tokens est.");
    }

    [Fact]
    public async Task BeginResponseProgressAsync_Should_UpdateEstimatedTokenCount_When_RequestIsStillRunning()
    {
        FakeConsoleTerminal terminal = new();
        ConsoleCliOutputTarget outputTarget = new(terminal);
        ConsoleReplOutputWriter sut = new(
            new MarkdownLikeCliMessageFormatter(),
            new CliTextRenderer(outputTarget),
            outputTarget,
            terminal);

        await using IAsyncDisposable progress = await sut.BeginResponseProgressAsync(14, 10, CancellationToken.None);
        await Task.Delay(350);

        terminal.Output.Should().Contain("\u2193 24 tokens est.");
        terminal.Output.Should().MatchRegex(@".*\u2193 2[5-9] tokens est\..*");
    }
}
