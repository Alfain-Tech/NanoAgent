using FinalAgent.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace FinalAgent.Tests.Infrastructure.Configuration;

public sealed class ApplicationOptionsValidatorTests
{
    private readonly ApplicationOptionsValidator _sut = new();

    [Fact]
    public void Validate_Should_ReturnSuccess_When_OptionsAreWithinSupportedRange()
    {
        ApplicationOptions options = new()
        {
            OperatorName = "FinalAgent",
            TargetName = "Native AOT host",
            RepeatCount = 3,
            DelayMilliseconds = 500
        };

        ValidateOptionsResult result = _sut.Validate(Options.DefaultName, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_ReturnFailure_When_RequiredValuesAreMissing()
    {
        ApplicationOptions options = new()
        {
            OperatorName = "   ",
            TargetName = string.Empty,
            RepeatCount = 1,
            DelayMilliseconds = 0
        };

        ValidateOptionsResult result = _sut.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("OperatorName"));
        result.Failures.Should().Contain(failure => failure.Contains("TargetName"));
    }

    [Fact]
    public void Validate_Should_ReturnFailure_When_NumericValuesAreOutOfRange()
    {
        ApplicationOptions options = new()
        {
            OperatorName = "FinalAgent",
            TargetName = "Native AOT host",
            RepeatCount = 0,
            DelayMilliseconds = 60001
        };

        ValidateOptionsResult result = _sut.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("RepeatCount"));
        result.Failures.Should().Contain(failure => failure.Contains("DelayMilliseconds"));
    }

    [Fact]
    public void Validate_Should_ThrowArgumentNullException_When_OptionsAreNull()
    {
        Action act = () => _sut.Validate(Options.DefaultName, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }
}
