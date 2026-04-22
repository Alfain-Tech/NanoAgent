namespace NanoAgent.Domain.Models;

public static class ProviderKindExtensions
{
    private const string OpenAiBaseUrl = "https://api.openai.com/v1";
    private const string GoogleAiStudioBaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai";

    public static string ToDisplayName(this ProviderKind providerKind)
    {
        return providerKind switch
        {
            ProviderKind.OpenAi => "OpenAI",
            ProviderKind.GoogleAiStudio => "Google AI Studio",
            ProviderKind.OpenAiCompatible => "OpenAI-compatible provider",
            _ => providerKind.ToString()
        };
    }

    public static string? GetManagedBaseUrl(this ProviderKind providerKind)
    {
        return providerKind switch
        {
            ProviderKind.OpenAi => OpenAiBaseUrl,
            ProviderKind.GoogleAiStudio => GoogleAiStudioBaseUrl,
            _ => null
        };
    }
}
