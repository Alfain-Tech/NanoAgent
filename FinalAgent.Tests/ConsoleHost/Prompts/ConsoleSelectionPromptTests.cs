using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Exceptions;
using FinalAgent.Application.Models;
using FinalAgent.ConsoleHost.Prompts;
using FinalAgent.ConsoleHost.Terminal;
using FinalAgent.Tests.ConsoleHost.TestDoubles;
using FluentAssertions;
using Moq;

namespace FinalAgent.Tests.ConsoleHost.Prompts;

public sealed class ConsoleSelectionPromptTests
{
    [Fact]
    public async Task PromptAsync_Should_ReturnSelectedValue_When_UserNavigatesWithArrowKeys()
    {
        FakeConsoleTerminal terminal = new();
        terminal.EnqueueKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        terminal.EnqueueKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        ConsolePromptRenderer renderer = new(terminal);
        Mock<IStatusMessageWriter> statusMessageWriter = new(MockBehavior.Strict);
        ConsoleSelectionPrompt sut = new(terminal, renderer, statusMessageWriter.Object);

        SelectionPromptRequest<string> request = new(
            "Choose a provider",
            [
                new SelectionPromptOption<string>("OpenAI", "openai"),
                new SelectionPromptOption<string>("Compatible", "compatible")
            ],
            "Pick the provider to configure.",
            DefaultIndex: 0);

        string selectedValue = await sut.PromptAsync(request, CancellationToken.None);

        selectedValue.Should().Be("compatible");
    }

    [Fact]
    public async Task PromptAsync_Should_UseFallbackDefault_When_InteractiveControlsAreUnavailable_And_InputIsBlank()
    {
        FakeConsoleTerminal terminal = new()
        {
            IsInputRedirected = true,
            IsOutputRedirected = true
        };
        terminal.EnqueueLine(string.Empty);

        ConsolePromptRenderer renderer = new(terminal);
        Mock<IStatusMessageWriter> statusMessageWriter = new(MockBehavior.Strict);
        ConsoleSelectionPrompt sut = new(terminal, renderer, statusMessageWriter.Object);

        SelectionPromptRequest<string> request = new(
            "Choose a provider",
            [
                new SelectionPromptOption<string>("OpenAI", "openai"),
                new SelectionPromptOption<string>("Compatible", "compatible")
            ],
            DefaultIndex: 1);

        string selectedValue = await sut.PromptAsync(request, CancellationToken.None);

        selectedValue.Should().Be("compatible");
    }

    [Fact]
    public async Task PromptAsync_Should_ThrowPromptCancelledException_When_EscapeIsPressed()
    {
        FakeConsoleTerminal terminal = new();
        terminal.EnqueueKey(new ConsoleKeyInfo('\0', ConsoleKey.Escape, false, false, false));

        ConsolePromptRenderer renderer = new(terminal);
        Mock<IStatusMessageWriter> statusMessageWriter = new(MockBehavior.Strict);
        ConsoleSelectionPrompt sut = new(terminal, renderer, statusMessageWriter.Object);

        SelectionPromptRequest<string> request = new(
            "Choose a provider",
            [new SelectionPromptOption<string>("OpenAI", "openai")]);

        Func<Task> action = () => sut.PromptAsync(request, CancellationToken.None);

        await action.Should().ThrowAsync<PromptCancelledException>();
    }
}
