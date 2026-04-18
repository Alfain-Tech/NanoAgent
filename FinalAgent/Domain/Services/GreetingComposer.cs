using FinalAgent.Domain.Abstractions;
using FinalAgent.Domain.Models;

namespace FinalAgent.Domain.Services;

internal sealed class GreetingComposer : IGreetingComposer
{
    public string Compose(GreetingContext context)
    {
        string operatorName = Normalize(context.OperatorName, nameof(GreetingContext.OperatorName));
        string targetName = Normalize(context.TargetName, nameof(GreetingContext.TargetName));
        string salutation = ResolveSalutation(context.OccurredAt);

        return $"[{context.OccurredAt:O}] {salutation}, {targetName}. This is {operatorName}.";
    }

    private static string Normalize(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }

    private static string ResolveSalutation(DateTimeOffset occurredAt)
    {
        return occurredAt.Hour switch
        {
            >= 5 and < 12 => "Good morning",
            >= 12 and < 18 => "Good afternoon",
            _ => "Good evening"
        };
    }
}
