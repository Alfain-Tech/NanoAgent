using System.Net.Http.Headers;
using System.Text.Json;
using NanoAgent.Application.Abstractions;
using NanoAgent.Application.Exceptions;
using NanoAgent.Domain.Models;
using NanoAgent.Infrastructure.OpenAi;

namespace NanoAgent.Infrastructure.Models;

internal sealed class OpenAiCompatibleModelProviderClient : IModelProviderClient
{
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

        Uri baseUri = OpenAiBaseUriResolver.Resolve(providerProfile);
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
    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength
            ? value
            : value[..Math.Max(0, maxLength - 3)] + "...";
    }
}
