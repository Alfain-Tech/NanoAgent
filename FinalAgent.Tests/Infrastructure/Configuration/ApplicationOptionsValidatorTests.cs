using FinalAgent.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace FinalAgent.Tests.Infrastructure.Configuration;

public sealed class ApplicationOptionsValidatorTests
{
    private readonly ApplicationOptionsValidator _sut = new();

    [Fact]
    public void Validate_Should_ReturnSuccess_When_ProductAndStorageDirectoryAreProvided()
    {
        ApplicationOptions options = new()
        {
            ProductName = "FinalAgent",
            StorageDirectoryName = "FinalAgent"
        };

        ValidateOptionsResult result = _sut.Validate(Options.DefaultName, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_ReturnFailure_When_RequiredValuesAreMissing()
    {
        ApplicationOptions options = new()
        {
            ProductName = "",
            StorageDirectoryName = " "
        };

        ValidateOptionsResult result = _sut.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("ProductName"));
        result.Failures.Should().Contain(failure => failure.Contains("StorageDirectoryName"));
    }

    [Fact]
    public void Validate_Should_ReturnFailure_When_StorageDirectoryContainsInvalidPathCharacters()
    {
        char invalidCharacter = Path.GetInvalidFileNameChars().First(character => character != '\0');
        ApplicationOptions options = new()
        {
            ProductName = "FinalAgent",
            StorageDirectoryName = $"Final{invalidCharacter}Agent"
        };

        ValidateOptionsResult result = _sut.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("invalid path characters"));
    }

    [Fact]
    public void Validate_Should_ThrowArgumentNullException_When_OptionsAreNull()
    {
        Action act = () => _sut.Validate(Options.DefaultName, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }
}
