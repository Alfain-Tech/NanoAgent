using System.Net.Http.Headers;
using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Exceptions;
using FinalAgent.Domain.Models;

namespace FinalAgent.Infrastructure.Models;

internal sealed class OpenAiCompatibleModelProviderClient : IModelProviderClient
{
    private const string OpenAiBaseUrl = "https://api.openai.com/v1/";

    private readonly HttpClient _httpClient;

    public OpenAiCompatibleModelProviderClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<AvailableModel>> GetAvailableModelsAsync(
        AgentProviderProfile providerProfile,
        string apiKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(providerProfile);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        Uri baseUri = ResolveBaseUri(providerProfile);
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri(baseUri, "models"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            string detail = string.IsNullOrWhiteSpace(errorBody)
                ? $"Provider returned HTTP {(int)response.StatusCode}."
                : $"Provider returned HTTP {(int)response.StatusCode}: {Truncate(errorBody.Trim(), 200)}";

            throw new ModelProviderException(
                $"Unable to fetch models from the configured provider. {detail}");
        }

        await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        ModelListResponse? payload = await JsonSerializer.DeserializeAsync(
            responseStream,
            ModelApiJsonContext.Default.ModelListResponse,
            cancellationToken);

        if (payload?.Data is null)
        {
            throw new ModelProviderException(
                "The configured provider returned an invalid models response.");
        }

        return payload.Data
            .Select(item => new AvailableModel(item.Id))
            .ToArray();
    }

    private static Uri ResolveBaseUri(AgentProviderProfile providerProfile)
    {
        string baseUrl = providerProfile.ProviderKind == ProviderKind.OpenAi
            ? OpenAiBaseUrl
            : providerProfile.BaseUrl
                ?? throw new ModelProviderException(
                    "The configured OpenAI-compatible provider is missing a base URL.");

        return new Uri(baseUrl.EndsWith("/", StringComparison.Ordinal)
            ? baseUrl
            : $"{baseUrl}/");
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength
            ? value
            : value[..Math.Max(0, maxLength - 3)] + "...";
    }
}
