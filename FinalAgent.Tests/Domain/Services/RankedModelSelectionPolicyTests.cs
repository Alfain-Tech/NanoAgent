using FinalAgent.Application.Exceptions;
using FinalAgent.Domain.Models;
using FinalAgent.Domain.Services;
using FluentAssertions;

namespace FinalAgent.Tests.Domain.Services;

public sealed class RankedModelSelectionPolicyTests
{
    private readonly RankedModelSelectionPolicy _sut = new();

    [Fact]
    public void Select_Should_UseConfiguredDefault_When_ItMatchesAvailableModels()
    {
        ModelSelectionContext context = new(
            [new AvailableModel("gpt-5"), new AvailableModel("gpt-5-mini")],
            "gpt-5-mini",
            ["gpt-5"]);

        ModelSelectionDecision result = _sut.Select(context);

        result.Should().Be(new ModelSelectionDecision(
            "gpt-5-mini",
            ModelSelectionSource.ConfiguredDefault,
            ConfiguredDefaultModelStatus.Matched,
            "gpt-5-mini"));
    }

    [Fact]
    public void Select_Should_UseRankedPreference_When_ConfiguredDefaultIsMissing()
    {
        ModelSelectionContext context = new(
            [new AvailableModel("gpt-4.1"), new AvailableModel("gpt-5-mini")],
            null,
            ["gpt-5", "gpt-5-mini"]);

        ModelSelectionDecision result = _sut.Select(context);

        result.Should().Be(new ModelSelectionDecision(
            "gpt-5-mini",
            ModelSelectionSource.RankedPreference,
            ConfiguredDefaultModelStatus.NotConfigured,
            null));
    }

    [Fact]
    public void Select_Should_UseRankedPreference_When_ConfiguredDefaultIsNotReturned()
    {
        ModelSelectionContext context = new(
            [new AvailableModel("gpt-4.1"), new AvailableModel("gpt-5-mini")],
            "gpt-5",
            ["gpt-4.1", "gpt-5-mini"]);

        ModelSelectionDecision result = _sut.Select(context);

        result.Should().Be(new ModelSelectionDecision(
            "gpt-4.1",
            ModelSelectionSource.RankedPreference,
            ConfiguredDefaultModelStatus.NotFound,
            "gpt-5"));
    }

    [Fact]
    public void Select_Should_ThrowModelSelectionException_When_NoRankedPreferenceMatches()
    {
        ModelSelectionContext context = new(
            [new AvailableModel("gpt-4.1-mini")],
            "gpt-5",
            ["gpt-4.1", "gpt-5-mini"]);

        Action act = () => _sut.Select(context);

        act.Should().Throw<ModelSelectionException>()
            .WithMessage("*None of the ranked preference models are available.*");
    }
}
