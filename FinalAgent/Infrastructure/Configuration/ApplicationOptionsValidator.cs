using Microsoft.Extensions.Options;

namespace FinalAgent.Infrastructure.Configuration;

public sealed class ApplicationOptionsValidator : IValidateOptions<ApplicationOptions>
{
    public ValidateOptionsResult Validate(string? name, ApplicationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.ProductName))
        {
            failures.Add($"{ApplicationOptions.SectionName}:ProductName must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.StorageDirectoryName))
        {
            failures.Add($"{ApplicationOptions.SectionName}:StorageDirectoryName must be provided.");
        }

        if (ContainsInvalidPathCharacters(options.StorageDirectoryName))
        {
            failures.Add($"{ApplicationOptions.SectionName}:StorageDirectoryName contains invalid path characters.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool ContainsInvalidPathCharacters(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
    }
}
