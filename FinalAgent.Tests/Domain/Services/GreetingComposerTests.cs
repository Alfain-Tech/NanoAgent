using FinalAgent.Domain.Models;
using FinalAgent.Domain.Services;
using FluentAssertions;

namespace FinalAgent.Tests.Domain.Services;

public sealed class GreetingComposerTests
{
    private readonly GreetingComposer _sut = new();

    [Fact]
    public void Compose_Should_ReturnMorningGreeting_When_OccurredAtIsBeforeNoon()
    {
        GreetingContext context = new(
            "FinalAgent",
            "Native AOT host",
            new DateTimeOffset(2026, 4, 19, 8, 30, 0, TimeSpan.Zero));

        string message = _sut.Compose(context);

        message.Should().Be("[2026-04-19T08:30:00.0000000+00:00] Good morning, Native AOT host. This is FinalAgent.");
    }

    [Fact]
    public void Compose_Should_ReturnAfternoonGreeting_When_OccurredAtIsAfterMidday()
    {
        GreetingContext context = new(
            "FinalAgent",
            "Native AOT host",
            new DateTimeOffset(2026, 4, 19, 13, 15, 0, TimeSpan.Zero));

        string message = _sut.Compose(context);

        message.Should().Be("[2026-04-19T13:15:00.0000000+00:00] Good afternoon, Native AOT host. This is FinalAgent.");
    }

    [Fact]
    public void Compose_Should_ReturnEveningGreeting_When_OccurredAtIsOutsideDaytimeHours()
    {
        GreetingContext context = new(
            "FinalAgent",
            "Native AOT host",
            new DateTimeOffset(2026, 4, 19, 21, 45, 0, TimeSpan.Zero));

        string message = _sut.Compose(context);

        message.Should().Be("[2026-04-19T21:45:00.0000000+00:00] Good evening, Native AOT host. This is FinalAgent.");
    }

    [Fact]
    public void Compose_Should_TrimNames_When_ContextContainsPadding()
    {
        GreetingContext context = new(
            "  FinalAgent  ",
            "  Native AOT host  ",
            new DateTimeOffset(2026, 4, 19, 8, 30, 0, TimeSpan.Zero));

        string message = _sut.Compose(context);

        message.Should().Be("[2026-04-19T08:30:00.0000000+00:00] Good morning, Native AOT host. This is FinalAgent.");
    }

    [Fact]
    public void Compose_Should_ThrowArgumentException_When_TargetNameIsWhitespace()
    {
        GreetingContext context = new(
            "FinalAgent",
            "   ",
            new DateTimeOffset(2026, 4, 19, 8, 30, 0, TimeSpan.Zero));

        Action act = () => _sut.Compose(context);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("TargetName");
    }
}
