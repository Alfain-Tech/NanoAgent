using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FinalAgent.Application.Abstractions;
using FinalAgent.Application.Exceptions;
using FinalAgent.Application.Models;
using FinalAgent.Infrastructure.OpenAi;

namespace FinalAgent.Infrastructure.Conversation;

internal sealed class OpenAiCompatibleConversationProviderClient : IConversationProviderClient
{
    private readonly HttpClient _httpClient;

    public OpenAiCompatibleConversationProviderClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ConversationProviderPayload> SendAsync(
        ConversationProviderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        Uri baseUri = OpenAiBaseUriResolver.Resolve(request.ProviderProfile);
        using HttpRequestMessage httpRequest = new(HttpMethod.Post, new Uri(baseUri, "chat/completions"));
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);

        OpenAiChatCompletionRequest payload = BuildRequestPayload(request);
        string requestBody = JsonSerializer.Serialize(
            payload,
            OpenAiConversationJsonContext.Default.OpenAiChatCompletionRequest);

        httpRequest.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string detail = string.IsNullOrWhiteSpace(responseBody)
                ? $"Provider returned HTTP {(int)response.StatusCode}."
                : $"Provider returned HTTP {(int)response.StatusCode}: {Truncate(responseBody.Trim(), 200)}";

            throw new ConversationProviderException(
                $"Unable to complete the conversation request. {detail}");
        }

        if (string.IsNullOrWhiteSpace(responseBody))
        {
            throw new ConversationProviderException(
                "The provider returned an empty response body for the conversation request.");
        }

        return new ConversationProviderPayload(
            request.ProviderProfile.ProviderKind,
            responseBody,
            TryGetResponseId(response));
    }

    private static OpenAiChatCompletionRequest BuildRequestPayload(ConversationProviderRequest request)
    {
        List<OpenAiChatCompletionRequestMessage> messages = [];

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new OpenAiChatCompletionRequestMessage(
                "system",
                request.SystemPrompt.Trim()));
        }

        messages.Add(new OpenAiChatCompletionRequestMessage(
            "user",
            request.UserInput.Trim()));

        OpenAiChatCompletionToolDefinition[] tools = request.AvailableTools
            .Select(definition => new OpenAiChatCompletionToolDefinition(
                "function",
                new OpenAiChatCompletionFunctionDefinition(
                    definition.Name,
                    definition.Description,
                    definition.Schema)))
            .ToArray();

        return new OpenAiChatCompletionRequest(
            request.ModelId,
            messages,
            tools);
    }

    private static string? TryGetResponseId(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("x-request-id", out IEnumerable<string>? requestIds))
        {
            return requestIds.FirstOrDefault();
        }

        return null;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength
            ? value
            : value[..Math.Max(0, maxLength - 3)] + "...";
    }
}
