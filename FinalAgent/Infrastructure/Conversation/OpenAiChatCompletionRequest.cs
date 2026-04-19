using System.Text.Json.Serialization;

namespace FinalAgent.Infrastructure.Conversation;

internal sealed record OpenAiChatCompletionRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<OpenAiChatCompletionRequestMessage> Messages);

internal sealed record OpenAiChatCompletionRequestMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);
