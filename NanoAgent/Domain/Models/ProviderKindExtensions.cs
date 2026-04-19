namespace NanoAgent.Domain.Models;

public static class ProviderKindExtensions
{
    public static string ToDisplayName(this ProviderKind providerKind)
    {
        return providerKind switch
        {
            ProviderKind.OpenAi => "OpenAI",
            ProviderKind.OpenAiCompatible => "OpenAI-compatible provider",
            _ => providerKind.ToString()
        };
    }
}
