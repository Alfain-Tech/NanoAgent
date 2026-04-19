using FinalAgent.ConsoleHost.Rendering;
using FinalAgent.Tests.ConsoleHost.TestDoubles;
using FluentAssertions;

namespace FinalAgent.Tests.ConsoleHost.Rendering;

public sealed class ConsoleCliOutputTargetTests
{
    [Fact]
    public void WriteLine_Should_FallBackToPlainText_When_OutputIsRedirected()
    {
        FakeConsoleTerminal terminal = new()
        {
            IsOutputRedirected = true
        };

        ConsoleCliOutputTarget sut = new(terminal);

        sut.WriteLine([
            new CliOutputSegment("assistant", CliOutputStyle.AssistantLabel),
            new CliOutputSegment(": hello", CliOutputStyle.AssistantText)
        ]);

        terminal.Output.Should().Be($"assistant: hello{Environment.NewLine}");
    }
}
