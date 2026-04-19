using FinalAgent.Domain.Models;
using FinalAgent.Domain.Services;

namespace FinalAgent.Infrastructure.OpenAi;

internal static class OpenAiBaseUriResolver
{
    private const string OpenAiBaseUrl = "https://api.openai.com/v1/";

    public static Uri Resolve(AgentProviderProfile providerProfile)
    {
        ArgumentNullException.ThrowIfNull(providerProfile);

        string baseUrl = providerProfile.ProviderKind == ProviderKind.OpenAi
            ? OpenAiBaseUrl
            : providerProfile.BaseUrl
                ?? throw new InvalidOperationException(
                    "The configured OpenAI-compatible provider is missing a base URL.");

        string normalizedBaseUrl = providerProfile.ProviderKind == ProviderKind.OpenAi
            ? baseUrl
            : CompatibleProviderBaseUrlNormalizer.Normalize(baseUrl);

        string baseUri = normalizedBaseUrl.EndsWith("/", StringComparison.Ordinal)
            ? normalizedBaseUrl
            : $"{normalizedBaseUrl}/";

        return new Uri(baseUri);
    }
}
