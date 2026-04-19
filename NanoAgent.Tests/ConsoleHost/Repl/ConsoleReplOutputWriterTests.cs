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
            outputTarget);

        await sut.WriteShellHeaderAsync("NanoAgent", "gpt-oss-20b", CancellationToken.None);

        terminal.Output.Should().Contain("NanoAgent");
        terminal.Output.Should().Contain("Model: gpt-oss-20b");
        terminal.Output.Should().Contain("GitHub: github.com/rizwan3d/NanoAgent");
        terminal.Output.Should().Contain("Sponsor: ALFAIN Technologies (PVT) Limited (https://alfain.co/)");
        terminal.Output.Should().Contain("Press Ctrl+C or use /exit to quit.");
        terminal.Output.Should().Contain(new string('\u2500', 53));
    }
}
