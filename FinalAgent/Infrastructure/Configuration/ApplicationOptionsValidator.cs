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

        if (options.Conversation is null)
        {
            failures.Add($"{ApplicationOptions.SectionName}:Conversation must be provided.");
        }
        else if (options.Conversation.RequestTimeoutSeconds <= 0)
        {
            failures.Add($"{ApplicationOptions.SectionName}:Conversation:RequestTimeoutSeconds must be greater than zero.");
        }

        if (options.ModelSelection is null)
        {
            failures.Add($"{ApplicationOptions.SectionName}:ModelSelection must be provided.");
        }
        else
        {
            if (options.ModelSelection.CacheDurationSeconds <= 0)
            {
                failures.Add($"{ApplicationOptions.SectionName}:ModelSelection:CacheDurationSeconds must be greater than zero.");
            }

            bool hasRankedPreferences = options.ModelSelection.RankedPreferenceList
                .Any(value => !string.IsNullOrWhiteSpace(value));

            if (!hasRankedPreferences)
            {
                failures.Add($"{ApplicationOptions.SectionName}:ModelSelection:RankedPreferenceList must contain at least one model identifier.");
            }
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
