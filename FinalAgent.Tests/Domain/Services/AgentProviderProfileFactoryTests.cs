using FinalAgent.Domain.Models;
using FinalAgent.Domain.Services;
using FluentAssertions;

namespace FinalAgent.Tests.Domain.Services;

public sealed class AgentProviderProfileFactoryTests
{
    private readonly AgentProviderProfileFactory _sut = new();

    [Fact]
    public void CreateOpenAi_Should_ReturnOpenAiProfile_When_Called()
    {
        AgentProviderProfile profile = _sut.CreateOpenAi();

        profile.Should().Be(new AgentProviderProfile(ProviderKind.OpenAi, null));
    }

    [Fact]
    public void CreateCompatible_Should_NormalizeTrailingSlash_When_BaseUrlIsProvided()
    {
        AgentProviderProfile profile = _sut.CreateCompatible(" https://provider.example.com/v1/ ");

        profile.Should().Be(new AgentProviderProfile(ProviderKind.OpenAiCompatible, "https://provider.example.com/v1"));
    }

    [Fact]
    public void CreateCompatible_Should_ThrowArgumentException_When_BaseUrlIsWhitespace()
    {
        Action act = () => _sut.CreateCompatible("  ");

        act.Should().Throw<ArgumentException>();
    }
}
