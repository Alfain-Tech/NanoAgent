using NanoAgent.Domain.Models;

namespace NanoAgent.Infrastructure.OpenAi;

internal static class OpenAiBaseUriResolver
{
    public static Uri Resolve(AgentProviderProfile providerProfile)
    {
        ArgumentNullException.ThrowIfNull(providerProfile);

        string resolvedBaseUrl = providerProfile.ResolveBaseUrl();
        string baseUri = resolvedBaseUrl.EndsWith("/", StringComparison.Ordinal)
            ? resolvedBaseUrl
            : $"{resolvedBaseUrl}/";

        return new Uri(baseUri);
    }
}
