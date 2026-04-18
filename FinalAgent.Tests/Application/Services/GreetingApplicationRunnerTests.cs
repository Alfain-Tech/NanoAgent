using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Services;
using FinalAgent.Domain.Abstractions;
using FinalAgent.Domain.Models;
using FinalAgent.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace FinalAgent.Tests.Application.Services;

public sealed class GreetingApplicationRunnerTests
{
    [Fact]
    public async Task RunAsync_Should_WriteGreetingForEachIteration_When_OptionsAreValid()
    {
        DateTimeOffset firstTimestamp = new(2026, 4, 19, 8, 0, 0, TimeSpan.Zero);
        DateTimeOffset secondTimestamp = new(2026, 4, 19, 8, 0, 1, TimeSpan.Zero);
        DateTimeOffset thirdTimestamp = new(2026, 4, 19, 8, 0, 2, TimeSpan.Zero);

        ApplicationOptions options = new()
        {
            OperatorName = "FinalAgent",
            TargetName = "AOT host",
            RepeatCount = 3,
            DelayMilliseconds = 0
        };

        List<GreetingContext> composedContexts = [];
        Queue<string> messagesToWrite = new(["message-1", "message-2", "message-3"]);
        List<string> writtenMessages = [];

        Mock<IGreetingComposer> greetingComposer = new(MockBehavior.Strict);
        greetingComposer
            .Setup(composer => composer.Compose(It.IsAny<GreetingContext>()))
            .Callback<GreetingContext>(context => composedContexts.Add(context))
            .Returns(() => messagesToWrite.Dequeue());

        Mock<IGreetingSink> greetingSink = new(MockBehavior.Strict);
        greetingSink
            .Setup(sink => sink.WriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((message, _) => writtenMessages.Add(message))
            .Returns(ValueTask.CompletedTask);

        Mock<ISystemClock> systemClock = new(MockBehavior.Strict);
        systemClock.SetupSequence(clock => clock.UtcNow)
            .Returns(firstTimestamp)
            .Returns(secondTimestamp)
            .Returns(thirdTimestamp);

        GreetingApplicationRunner sut = CreateSut(
            greetingComposer.Object,
            greetingSink.Object,
            systemClock.Object,
            options);

        await sut.RunAsync(CancellationToken.None);

        writtenMessages.Should().Equal("message-1", "message-2", "message-3");
        composedContexts.Should().BeEquivalentTo(
            [
                new GreetingContext("FinalAgent", "AOT host", firstTimestamp),
                new GreetingContext("FinalAgent", "AOT host", secondTimestamp),
                new GreetingContext("FinalAgent", "AOT host", thirdTimestamp)
            ]);

        greetingComposer.Verify(
            composer => composer.Compose(It.IsAny<GreetingContext>()),
            Times.Exactly(options.RepeatCount));
        greetingSink.Verify(
            sink => sink.WriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(options.RepeatCount));
    }

    [Fact]
    public async Task RunAsync_Should_ThrowOperationCanceledException_When_CancellationIsAlreadyRequested()
    {
        ApplicationOptions options = new()
        {
            OperatorName = "FinalAgent",
            TargetName = "AOT host",
            RepeatCount = 1,
            DelayMilliseconds = 0
        };

        Mock<IGreetingComposer> greetingComposer = new(MockBehavior.Strict);
        Mock<IGreetingSink> greetingSink = new(MockBehavior.Strict);
        Mock<ISystemClock> systemClock = new(MockBehavior.Strict);

        GreetingApplicationRunner sut = CreateSut(
            greetingComposer.Object,
            greetingSink.Object,
            systemClock.Object,
            options);

        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        Func<Task> act = () => sut.RunAsync(cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        greetingComposer.VerifyNoOtherCalls();
        greetingSink.VerifyNoOtherCalls();
        systemClock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RunAsync_Should_PropagateException_When_GreetingSinkFails()
    {
        DateTimeOffset timestamp = new(2026, 4, 19, 8, 0, 0, TimeSpan.Zero);
        ApplicationOptions options = new()
        {
            OperatorName = "FinalAgent",
            TargetName = "AOT host",
            RepeatCount = 1,
            DelayMilliseconds = 0
        };

        InvalidOperationException expectedException = new("Sink failure.");

        Mock<IGreetingComposer> greetingComposer = new(MockBehavior.Strict);
        greetingComposer
            .Setup(composer => composer.Compose(It.IsAny<GreetingContext>()))
            .Returns("message-1");

        Mock<IGreetingSink> greetingSink = new(MockBehavior.Strict);
        greetingSink
            .Setup(sink => sink.WriteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromException(expectedException));

        Mock<ISystemClock> systemClock = new(MockBehavior.Strict);
        systemClock.Setup(clock => clock.UtcNow).Returns(timestamp);

        GreetingApplicationRunner sut = CreateSut(
            greetingComposer.Object,
            greetingSink.Object,
            systemClock.Object,
            options);

        Func<Task> act = () => sut.RunAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Sink failure.");
    }

    private static GreetingApplicationRunner CreateSut(
        IGreetingComposer greetingComposer,
        IGreetingSink greetingSink,
        ISystemClock systemClock,
        ApplicationOptions options)
    {
        return new GreetingApplicationRunner(
            greetingComposer,
            greetingSink,
            systemClock,
            Options.Create(options),
            NullLogger<GreetingApplicationRunner>.Instance);
    }
}
