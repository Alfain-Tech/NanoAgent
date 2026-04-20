using Microsoft.Extensions.Options;

namespace NanoAgent.Infrastructure.Configuration;

public sealed class ApplicationOptionsValidator : IValidateOptions<ApplicationOptions>
{
    public ValidateOptionsResult Validate(string? name, ApplicationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (options.Conversation is null)
        {
            failures.Add($"{ApplicationOptions.SectionName}:Conversation must be provided.");
        }
        else if (options.Conversation.RequestTimeoutSeconds < 0)
        {
            failures.Add($"{ApplicationOptions.SectionName}:Conversation:RequestTimeoutSeconds must be zero or greater.");
        }
        else if (options.Conversation.MaxHistoryTurns < 0)
        {
            failures.Add($"{ApplicationOptions.SectionName}:Conversation:MaxHistoryTurns must be zero or greater.");
        }
        else if (options.Conversation.MaxToolRoundsPerTurn <= 0)
        {
            failures.Add($"{ApplicationOptions.SectionName}:Conversation:MaxToolRoundsPerTurn must be greater than zero.");
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
        }

        if (options.Permissions is null)
        {
            failures.Add($"{ApplicationOptions.SectionName}:Permissions must be provided.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
